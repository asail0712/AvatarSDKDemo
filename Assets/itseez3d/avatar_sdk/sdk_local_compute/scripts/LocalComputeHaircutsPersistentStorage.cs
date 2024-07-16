/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, October 2019
*/

using ItSeez3D.AvatarSdk.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SimpleJSON;

namespace ItSeez3D.AvatarSdk.LocalCompute
{
	public class LocalComputeHaircutsPersistentStorage : IHaircutsPersistentStorage
	{
		#region poorMansCache
		class SimpleCacheEntry
		{
			private string avatarCode;
			public string AvatarCode
			{
				get { return avatarCode; }
				set
				{
					if (value == null) { avatarCode = ""; }
					else { avatarCode = value; }
				}
			}
			public Dictionary<string, HaircutMetadata> haircutMetadatas = new Dictionary<string, HaircutMetadata>();
			public HaircutMetadata GetHaircutMetadata(string haircutId)
			{
				if (haircutMetadatas.ContainsKey(haircutId))
				{
					return haircutMetadatas[haircutId];
				}
				return null;
			}
		}

		class SimpleCache
		{
			int currentFreeSlot;
			private SimpleCacheEntry[] cacheArray;
			private Dictionary<string, int> avatarCodesDictionary;
			public SimpleCache(int capacity)
			{
				cacheArray = new SimpleCacheEntry[capacity];
				avatarCodesDictionary = new Dictionary<string, int>();
				currentFreeSlot = 0;
			}
			private void MoveCurFreeSlot()
			{
				currentFreeSlot = (currentFreeSlot + 1) % cacheCapacity;
			}
			public void Add(SimpleCacheEntry element)
			{
				if (cacheArray[currentFreeSlot] != null)
				{
					avatarCodesDictionary.Remove(cacheArray[currentFreeSlot].AvatarCode);
				}
				cacheArray[currentFreeSlot] = element;
				avatarCodesDictionary.Add(element.AvatarCode, currentFreeSlot);
				MoveCurFreeSlot();
				if (cacheArray[currentFreeSlot] != null && cacheArray[currentFreeSlot].AvatarCode == "") // do not overwrite common cache
				{
					MoveCurFreeSlot();
				}
			}

			public bool Has(string avatarCode)
			{
				var avatarCodeToSearch = avatarCode == null ? "" : avatarCode;
				return avatarCodesDictionary.ContainsKey(avatarCodeToSearch);
			}

			public SimpleCacheEntry Get(string avatarCode)
			{
				string searchCode = string.IsNullOrEmpty(avatarCode) ? "" : avatarCode;
				return Has(searchCode) ? cacheArray[avatarCodesDictionary[searchCode]] : null;
			}
		}
		#endregion

		const int cacheCapacity = 5;
		SimpleCache cache = new SimpleCache(cacheCapacity);
		public bool UseCache = true;

		public string GetCommonHaircutsDirectory()
		{
			return persistentStorage.EnsureDirectoryExists(Path.Combine(persistentStorage.GetDataDirectory(), "haircuts"));
		}

		/// <summary>
		/// Get all of available haircut metadata (mostly paths to different files that represent haircut)
		/// </summary>
		/// <param name="haircutId">Id of haircut metadata requested for</param>
		/// <param name="avatarCode">Avatar code (may be null if metadata is requested for common haircut that does not belong to any avatar)</param>
		/// <returns>Structure with available haircut storage metadata</returns>
		public HaircutMetadata GetHaircutMetadata(string haircutId, string avatarCode)
		{
			if (UseCache)
			{
				if (cache.Has(avatarCode))
				{
					var metadata = cache.Get(avatarCode).GetHaircutMetadata(haircutId);
					if (metadata != null) { return metadata; }
				}
				else
				{
					cache.Add(new SimpleCacheEntry() { AvatarCode = avatarCode });
				}
			}

			HaircutMetadata result = null;
			//if avatarCode is null, we work only with common haircuts (e.g. no 'generated' avatar-specific ones)
			bool commonHaircutsOnly = avatarCode == null;

			if (!commonHaircutsOnly)
			{
				//try to read haircuts from "haircuts.json" file in avatar data directory
				var haircutsMetadata = ReadHaircutsMetadataFromFile(avatarCode);
				result = haircutsMetadata.Where(m => m.FullId == haircutId || m.ShortId == haircutId).FirstOrDefault();
			}

			if (result == null)
			{
				result = new HaircutMetadata();
				result.FullId = haircutId;
				result.ShortId = CoreTools.GetShortHaircutId(haircutId);
			}

			//PointCloud is mandatory for common haircuts, generated (avatar specific) haircut does not have one(it is already fit to head)
			if (commonHaircutsOnly || result.PathToPointCloud != null)
			{
				result.MeshPly = Path.Combine(GetCommonHaircutsDirectory(), string.Format("{0}.ply", result.FullId));
				result.Texture = Path.Combine(GetCommonHaircutsDirectory(), string.Format("{0}_model.png", result.FullId));
				result.Preview = Path.Combine(GetCommonHaircutsDirectory(), string.Format("{0}_preview.png", result.FullId));
			}
			else
			{
				string avatarHaircutsDirectory = Path.Combine(persistentStorage.GetAvatarDirectory(avatarCode), "haircuts"); //files are located at local avatar directory
				result.MeshPly = Path.Combine(avatarHaircutsDirectory, string.Format("{0}.ply", result.ShortId));
				result.Texture = Path.Combine(avatarHaircutsDirectory, string.Format("{0}.png", result.ShortId));
				result.Preview = null; //no previews for generated haircut
			}

			if (UseCache)
			{
				cache.Get(avatarCode).haircutMetadatas.Add(haircutId, result);
			}

			return result;
		}

		private IPersistentStorage persistentStorage = null;
		public void SetPersistentStorage(IPersistentStorage storage)
		{
			persistentStorage = storage;
		}

		public List<HaircutMetadata> ReadHaircutsMetadataFromFile(string avatarCode)
		{
			string avatarDataDirectory = persistentStorage.GetAvatarDirectory(avatarCode);
			string listOfHaircutsJsonFilename = persistentStorage.GetAvatarFilename(avatarCode, AvatarFile.HAIRCUTS_JSON);
			List<HaircutMetadata> haircutMetadatas = new List<HaircutMetadata>();
			try
			{
				if (File.Exists(listOfHaircutsJsonFilename))
				{
					var jsonContent = JSON.Parse(File.ReadAllText(listOfHaircutsJsonFilename));
					foreach (JSONNode haircutNameJson in jsonContent.Keys)
					{
						string haircutId = haircutNameJson.Value.ToString().Replace("\"", "");
						var haircutPathJson = jsonContent[haircutId];
						haircutId = haircutId.Replace('\\', '/');
						bool isPointCloud = PipelineTraitsFactory.Instance.GetTraitsFromAvatarCode(avatarCode).IsPointcloudApplicableToHaircut(haircutId);
						var metadata = new HaircutMetadata() { FullId = haircutId, ShortId = CoreTools.GetShortHaircutId(haircutId) };

						string localPathToFile = haircutPathJson.ToString().Replace("\"", "").Replace("\\\\", "\\");
						string pathToFile = Path.Combine(avatarDataDirectory, localPathToFile);

						if (isPointCloud)
						{
							metadata.PathToPointCloud = pathToFile;
						}
						else
						{
							metadata.MeshPly = pathToFile;
						}
						haircutMetadatas.Add(metadata);
					}
				}
			}
			catch (Exception exc)
			{
				Debug.LogErrorFormat("Unable to read haircuts json file: {0}", exc);
			}
			return haircutMetadatas;
		}
	}
}
