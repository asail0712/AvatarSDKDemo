/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using SimpleJSON;

namespace ItSeez3D.AvatarSdk.Core
{
	/// <summary>
	/// Predefined subdirectories inside avatar folder.
	/// </summary>
	public enum AvatarSubdirectory
	{
		BLENDSHAPES,
		OBJ_EXPORT,
		FBX_EXPORT,
		LOD,
	}

	/// <summary>
	/// "Types" of files encountered during avatar generation and loading.
	/// </summary>
	public enum AvatarFile
	{
		PHOTO,
		THUMBNAIL,
		MESH_PLY,
		MESH_GLTF,
		MESH_ZIP,
		TEXTURE,
		METALLIC_MAP_TEXTURE,
		ROUGHNESS_MAP_TEXTURE,
		NORMAL_MAP_TEXTURE,
		HAIRCUT_POINT_CLOUD_PLY,
		HAIRCUT_POINT_CLOUD_ZIP,
		ALL_HAIRCUT_POINTS_ZIP,
		HAIRCUTS_JSON,
		BLEDNSHAPES_JSON,
		BLENDSHAPES_ZIP,
		BLENDSHAPES_FBX_ZIP,
		BLENDSHAPES_PLY_ZIP,
		PARAMETERS_JSON,
		PIPELINE_INFO,
		MODEL_JSON
	}

	/// <summary>
	/// "Types" of files encountered during haircut loading.
	/// </summary>
	public enum HaircutFile
	{
		HAIRCUT_MESH_PLY,
		HAIRCUT_MESH_ZIP,
		HAIRCUT_TEXTURE,
		HAIRCUT_PREVIEW
	}

	/// <summary>
	/// SDK uses this interface to interact with the filesystem, e.g. save/load files and metadata.
	/// By default SDK will use DefaultPersistentStorage implementation. If your application stores files differently
	/// you can implement this interface and pass instance of your implementation to AvatarSdkMgr.Init() - this
	/// will override the default behavior. Probably the best way to implement IPersistentStorage is to derive from
	/// DefaultPersistentStorage.
	/// </summary>
	public abstract class IPersistentStorage
	{
		public abstract Dictionary<AvatarSubdirectory, string> AvatarSubdirectories { get; }

		public abstract Dictionary<AvatarFile, string> AvatarFilenames { get; }

		public abstract Dictionary<HaircutFile, string> HaircutFilenames { get; }

		public abstract string EnsureDirectoryExists (string d);

		public abstract string GetDataDirectory ();

		public abstract string GetResourcesDirectory ();

		public abstract string GetAvatarsDirectory ();

		public abstract string GetAvatarDirectory (string avatarCode, int levelOfDetails = 0);

		public abstract string GetAvatarSubdirectory (string avatarCode, AvatarSubdirectory dir, int levelOfDetails = 0);

		public abstract string GetAvatarFilename (string avatarCode, AvatarFile file, int levelOfDetails = 0);

		/// <summary>
		/// Returns avatar texture filename or additional texture filename if it is specified
		/// </summary>
		public abstract string GetAvatarTextureFilename(string avatarCode, string additionalTextureName = null);

		public abstract List<string> GetAvatarBlendshapesDirs(string avatarCode, int levelOfDetails = 0);

		public abstract string GetAvatarBlendshapesRootDir(string avatarCode, int levelOfDetails = 0);

		public abstract List<string> GetFullBlendshapesNames(string avatarCode);

		public abstract void StorePlayerUID (string identifier, string uid);

		public abstract string LoadPlayerUID (string identifier);
	}

	/// <summary>
	/// Default implementation of IPersistentStorage.
	/// </summary>
	public class DefaultPersistentStorage : IPersistentStorage
	{
		#region data members
		private Dictionary<AvatarSubdirectory, string> avatarSubdirectories = new Dictionary<AvatarSubdirectory, string> () {
			{ AvatarSubdirectory.BLENDSHAPES, "blendshapes" },
			{ AvatarSubdirectory.OBJ_EXPORT, "obj" },
			{ AvatarSubdirectory.FBX_EXPORT, "fbx" },
			{ AvatarSubdirectory.LOD, "LOD{0}" },
		};

		private Dictionary<AvatarFile, string> avatarFiles = new Dictionary<AvatarFile, string> () {
			{ AvatarFile.PHOTO, "photo.jpg" },
			{ AvatarFile.THUMBNAIL, "thumbnail.jpg" },
			{ AvatarFile.MESH_PLY, "model.ply" },
			{ AvatarFile.MESH_GLTF, "model.gltf" },
			{ AvatarFile.MESH_ZIP, "model.zip" },
			{ AvatarFile.TEXTURE, "model.jpg" },
			{ AvatarFile.METALLIC_MAP_TEXTURE, "metallic_map.jpg" },
			{ AvatarFile.ROUGHNESS_MAP_TEXTURE, "roughness_map.jpg" },
			{ AvatarFile.NORMAL_MAP_TEXTURE, "normal_map.png" },
			{ AvatarFile.HAIRCUT_POINT_CLOUD_PLY, "cloud_{0}.ply" },
			{ AvatarFile.HAIRCUT_POINT_CLOUD_ZIP, "{0}_points.zip" },
			{ AvatarFile.ALL_HAIRCUT_POINTS_ZIP, "all_haircut_points.zip" },
			{ AvatarFile.HAIRCUTS_JSON, "haircuts.json" },
			{ AvatarFile.BLEDNSHAPES_JSON, "blendshapes.json" },
			{ AvatarFile.BLENDSHAPES_ZIP, "blendshapes.zip" },
			{ AvatarFile.BLENDSHAPES_FBX_ZIP, "blendshapes_fbx.zip" },
			{ AvatarFile.BLENDSHAPES_PLY_ZIP, "blendshapes_ply.zip" },
			{ AvatarFile.PARAMETERS_JSON, "parameters.json"},
			{ AvatarFile.PIPELINE_INFO, "pipeline.txt"},
			{ AvatarFile.MODEL_JSON, "model.json"}
		};

		private Dictionary<HaircutFile, string> haircutFiles = new Dictionary<HaircutFile, string> () {
			{ HaircutFile.HAIRCUT_MESH_PLY, "{0}.ply" },  // corresponds to file name inside zip
			{ HaircutFile.HAIRCUT_MESH_ZIP, "{0}_model.zip" },
			{ HaircutFile.HAIRCUT_TEXTURE, "{0}_model.png" },
			{ HaircutFile.HAIRCUT_PREVIEW, "{0}_preview.png"},
		};

		private string dataRoot = string.Empty;

		#endregion

		#region implemented abstract members of IPersistentStorage

		public override Dictionary<AvatarSubdirectory, string> AvatarSubdirectories { get { return avatarSubdirectories; } }

		public override Dictionary<AvatarFile, string> AvatarFilenames { get { return avatarFiles; } }

		public override Dictionary<HaircutFile, string> HaircutFilenames { get { return haircutFiles; } }

		public override string EnsureDirectoryExists (string d)
		{
			if (!Directory.Exists (d))
				Directory.CreateDirectory (d);
			return d;
		}

		/// <summary>
		/// Native plugins do not currently support non-ASCII file paths. Therefore we must choose
		/// location that only contains ASCII characters in its path and is read-write accessible.
		/// This function will try different options before giving up.
		/// </summary>
		public override string GetDataDirectory ()
		{
			if (string.IsNullOrEmpty (dataRoot)) {
				var options = new string[] {
					Application.persistentDataPath,
					#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
					IOUtils.CombinePaths (Environment.GetFolderPath (Environment.SpecialFolder.CommonApplicationData), "avatar_sdk"),
					IOUtils.CombinePaths ("C:\\", "avatar_sdk_data"),
					#endif
					IOUtils.CombinePaths (Application.dataPath, "..", "avatar_sdk"),
				};

				for (int i = 0; i < options.Length; ++i) {
					Debug.LogFormat ("Trying {0} as data root...", options [i]);
					if (Utils.HasNonAscii (options [i])) {
						Debug.LogWarningFormat ("Data path \"{0}\" contains non-ASCII characters, trying next option...", options [i]);
						continue;
					}

					try {
						// make sanity checks to make sure we actually have read-write access to the directory
						EnsureDirectoryExists (options [i]);
						var testFilePath = Path.Combine (options [i], "test.file");
						File.WriteAllText (testFilePath, "test");
						File.ReadAllText (testFilePath);
						File.Delete (testFilePath);
					} catch (Exception ex) {
						Debug.LogException (ex);
						Debug.LogWarningFormat ("Could not access {0}, trying next option...", options [i]);
						continue;
					}

					dataRoot = options [i];
					break;
				}
			}

			if (string.IsNullOrEmpty (dataRoot))
				throw new Exception ("Could not find directory for persistent data! See log for details.");

			return EnsureDirectoryExists (dataRoot);
		}

		public override string GetResourcesDirectory ()
		{
			return EnsureDirectoryExists (Path.Combine (GetDataDirectory (), "resources"));
		}

		public override string GetAvatarsDirectory ()
		{
			return EnsureDirectoryExists (Path.Combine (GetDataDirectory (), "avatars"));
		}

		public override string GetAvatarDirectory (string avatarCode, int levelOfDetails = 0)
		{
			string avatarDirectory = Path.Combine(GetAvatarsDirectory(), avatarCode);
			if (levelOfDetails != 0)
				avatarDirectory = Path.Combine(avatarDirectory, GetLodDirectoryName(levelOfDetails));
			return EnsureDirectoryExists (avatarDirectory);
		}

		public override string GetAvatarSubdirectory (string avatarCode, AvatarSubdirectory dir, int levelOfDetails)
		{
			return EnsureDirectoryExists (Path.Combine (GetAvatarDirectory (avatarCode, levelOfDetails), AvatarSubdirectories [dir]));
		}

		public override string GetAvatarFilename (string avatarCode, AvatarFile file, int levelOfDetails = 0)
		{
			return Path.Combine (GetAvatarDirectory(avatarCode, levelOfDetails), AvatarFilenames [file]);
		}

		public override string GetAvatarTextureFilename(string avatarCode, string additionalTextureName = null)
		{
			if (string.IsNullOrEmpty(additionalTextureName))
				return GetAvatarFilename(avatarCode, AvatarFile.TEXTURE);
			else
			{
				string pngFile = Path.Combine(GetAvatarDirectory(avatarCode), additionalTextureName + ".png");
				if (File.Exists(pngFile))
					return pngFile;
				return Path.Combine(GetAvatarDirectory(avatarCode), additionalTextureName + ".jpg");
			}
		}
 
		private string PlayerUIDFilename (string identifier)
		{
			var filename = string.Format ("player_uid_{0}.dat", identifier);
			var path = Path.Combine (GetDataDirectory (), filename);
			return path;
		}

		public override void StorePlayerUID (string identifier, string uid)
		{
			try {
				Debug.LogFormat ("Storing player UID: {0}", uid);
				var uidText = Convert.ToBase64String (UTF8Encoding.UTF8.GetBytes (uid));
				var path = PlayerUIDFilename (identifier);
				File.WriteAllText (path, uidText);
			} catch (Exception ex) {
				Debug.LogErrorFormat ("Could not store player UID in a file, msg: {0}", ex.Message);
			}
		}

		public override string LoadPlayerUID (string identifier)
		{
			try {
				var path = PlayerUIDFilename (identifier);
				if (!File.Exists (path))
					return null;
				return UTF8Encoding.UTF8.GetString (Convert.FromBase64String (File.ReadAllText (path)));
			} catch (Exception ex) {
				Debug.LogWarningFormat ("Could not read player_uid from file: {0}", ex.Message);
				return null;
			}
		}

		public override List<string> GetAvatarBlendshapesDirs(string avatarCode, int levelOfDetails = 0)
		{
			List<string> blendshapesDirs = new List<string>();
			try
			{
				string blendshapesJsonFilename = GetAvatarFilename(avatarCode, AvatarFile.BLEDNSHAPES_JSON);
				if (File.Exists(blendshapesJsonFilename))
				{
					var jsonContent = JSON.Parse(File.ReadAllText(blendshapesJsonFilename));
					foreach (JSONNode blendshapesNameJson in jsonContent.Keys)
					{
						string blendshapesId = blendshapesNameJson.Value.ToString().Replace("\"", "");
						var blendshapesPathJson = jsonContent[blendshapesId];
						blendshapesDirs.Add(Path.Combine(GetAvatarDirectory(avatarCode, levelOfDetails), blendshapesPathJson.ToString().Replace("\"", "").Replace("\\\\", "\\")));
					}
				}
				else
				{
					blendshapesDirs.Add(GetAvatarBlendshapesRootDir(avatarCode, levelOfDetails));
				}
			}
			catch (Exception exc)
			{
				Debug.LogErrorFormat("Unable to read blendshapes json file: {0}", exc);
			}
			return blendshapesDirs;
		}

		public override string GetAvatarBlendshapesRootDir(string avatarCode, int levelOfDetails = 0)
		{
			return Path.Combine(GetAvatarDirectory(avatarCode, levelOfDetails), avatarSubdirectories[AvatarSubdirectory.BLENDSHAPES]);
		}

		public override List<string> GetFullBlendshapesNames(string avatarCode)
		{
			List<string> blendshapesNames = new List<string>();
			List<string> blendshapesDirs = GetAvatarBlendshapesDirs(avatarCode);
			string blendshapesRootDir = GetAvatarBlendshapesRootDir(avatarCode);
			foreach(string dir in blendshapesDirs)
			{
				if (Directory.Exists(dir))
				{
					string blendshapeNamePrefix = string.Empty;
					if (dir.IndexOf(blendshapesRootDir) == 0)
						blendshapeNamePrefix = dir.Substring(blendshapesRootDir.Length);
					string[] blendshapesFiles = Directory.GetFiles(dir);
					foreach (string blendshapeFile in blendshapesFiles)
					{
						if (blendshapeFile.EndsWith(".bin"))
						{
							string name = Path.GetFileNameWithoutExtension(blendshapeFile);
							if (!string.IsNullOrEmpty(blendshapeNamePrefix))
								name = blendshapeNamePrefix + Path.DirectorySeparatorChar + name;
							blendshapesNames.Add(name);
						}
					}
				}
			}
			return blendshapesNames;
		}

		#endregion

		private string GetLodDirectoryName(int lod)
		{
			return string.Format(avatarSubdirectories[AvatarSubdirectory.LOD], lod);
		}
	}
}

