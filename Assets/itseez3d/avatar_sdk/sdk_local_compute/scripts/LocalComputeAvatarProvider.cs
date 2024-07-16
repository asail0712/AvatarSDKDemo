/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

#if !UNITY_WEBGL
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ItSeez3D.AvatarSdk.Core;
using System.IO;
using System.Text;

namespace ItSeez3D.AvatarSdk.LocalCompute
{
	/// <summary>
	/// Implementation of the IAvatarProvider for local compute version of the Avatar SDK.
	/// </summary>
	public class LocalComputeAvatarProvider : IAvatarProvider
	{
		#region Data
		// SDK session.
		protected Session session = null;


		#endregion

		#region Ctor

		public LocalComputeAvatarProvider()
		{
			session = new Session();
		}

		public LocalComputeAvatarProvider(Session session)
		{
			this.session = session;
		}
		#endregion

		#region IAvatarProvider

		public bool IsInitialized { get { return session.IsInitialized; } }

		/// <summary>
		/// Preload resources early at a cost of some RAM.
		/// </summary>
		public AsyncRequest InitializeAsync()
		{
			return session.InitializeAsync();
		}

		/// <summary>
		/// Initialize avatar and prepare for computations.
		/// </summary>
		/// <param name="photoBytes">Photo bytes (png or jpg encoded)</param>
		/// <param name="name">Currently not used by local compute SDK</param>
		/// <param name="description">Currently not used by local compute SDK</param>
		/// <param name="pipeline">Calculation pipeline to use</param>
		/// <param name="computationParameters">Computation parameters</param>
		/// <returns>Unique avatar code</returns>
		public AsyncRequest<string> InitializeAvatarAsync(byte[] photoBytes, string name, string description, PipelineType pipeline,
			ComputationParameters computationParameters = null)
		{			
			var request = new AsyncRequest<string>(AvatarSdkMgr.Str(Strings.GeneratingAvatar));
			AvatarSdkMgr.SpawnCoroutine(InitializeAvatarFunc(photoBytes, computationParameters, request, pipeline));
			return request;
		}

		/// <summary>
		/// Start avatar computations and wait until completed
		/// </summary>
		public AsyncRequest StartAndAwaitAvatarCalculationAsync(string avatarCode)
		{
			var request = new AsyncRequest(AvatarSdkMgr.Str(Strings.GeneratingAvatar));
			PipelineType pipeLineType = CoreTools.LoadPipelineType(avatarCode);
			AvatarSdkMgr.SpawnCoroutine(StartAndAwaitAvatarCalculationFunc(avatarCode, pipeLineType, request));
			return request;
		}

		/// <summary>
		/// Generates additional haircuts for existing avatar.
		/// </summary>
		public AsyncRequest GenerateHaircutsAsync(string avatarCode, List<string> haircutsList)
		{
			var request = new AsyncRequest(AvatarSdkMgr.Str(Strings.GeneratingHaircuts));
			AvatarSdkMgr.SpawnCoroutine(GenerateHaircutsFunc(avatarCode, haircutsList, request));
			return request;
		}

		/// <summary>
		/// Do nothing. For local compute SDK all data is already stored locally.
		/// </summary>
		public AsyncRequest MoveAvatarModelToLocalStorageAsync(string avatarCode, bool withHaircutPointClouds, bool withBlendshapes, MeshFormat format = MeshFormat.PLY)
		{
			return new AsyncRequest() { IsDone = true };
		}

		/// <summary>
		/// Creates TexturedMesh of the head for a given avatar.
		/// </summary>
		/// <param name="avatarCode">Code of the avatar</param>
		/// <param name="withBlendshapes">Blendshapes will be added to mesh</param>
		/// <param name="detailsLevel">Details level of the mesh</param>
		/// <param name="additionalTextureName">Name of the texture that should be applied intead of default</param>
		public AsyncRequest<TexturedMesh> GetHeadMeshAsync(string avatarCode, bool withBlendshapes, int detailsLevel = 0, MeshFormat format = MeshFormat.PLY, 
			string additionalTextureName = null)
		{
			var request = new AsyncRequest<TexturedMesh>(AvatarSdkMgr.Str(Strings.LoadingHeadMesh));
			AvatarSdkMgr.SpawnCoroutine(GetHeadMeshFunc(avatarCode, withBlendshapes, detailsLevel, additionalTextureName, request));
			return request;
		}

		/// <summary>
		/// Returns avatar texture by name or standard texture if name isn't specified. 
		/// If the textures doesn't exist, it will be downloaded from the cloud
		/// </summary>
		/// <param name="avatarCode">avatar code</param>
		/// <param name="textureName">Texture name or pass null for standard texture.</param>
		public AsyncRequest<Texture2D> GetTextureAsync(string avatarCode, string textureName)
		{
			var request = new AsyncRequest<Texture2D>(AvatarSdkMgr.Str(Strings.LoadingTexture));
			AvatarSdkMgr.SpawnCoroutine(GetTextureFunc(avatarCode, textureName, request));
			return request;
		}

		/// <summary>
		/// Returns identities of all haircuts available for the avatar
		/// </summary>
		public AsyncRequest<string[]> GetHaircutsIdAsync(string avatarCode)
		{
			var request = new AsyncRequest<string[]>(AvatarSdkMgr.Str(Strings.GettingAvailableHaircuts));
			AvatarSdkMgr.SpawnCoroutine(GetHaircutsIdFunc(avatarCode, request));
			return request;
		}

		/// <summary>
		/// Creates TexturedMesh of the haircut.
		/// </summary>
		/// <param name="avatarCode">Avatar code</param>
		/// <param name="haircutName">Haircut identity</param>
		public AsyncRequest<TexturedMesh> GetHaircutMeshAsync(string avatarCode, string haircutId)
		{
			var request = new AsyncRequest<TexturedMesh>(AvatarSdkMgr.Str(Strings.GettingHaircutMesh));
			AvatarSdkMgr.SpawnCoroutine(GetHaircutMeshFunc(avatarCode, haircutId, request));
			return request;
		}

		/// <summary>
		/// Reads haircut preview image and returns its bytes, or extract it from the resources if it doesn't exist.
		/// </summary>
		/// <param name="haircutId">haircut identity</param>
		public AsyncRequest<byte[]> GetHaircutPreviewAsync(string avatarCode, string haircutId)
		{
			var request = new AsyncRequest<byte[]>(AvatarSdkMgr.Str(Strings.GettingHaircutPreview));
			AvatarSdkMgr.SpawnCoroutine(GetHaircutPreviewFunc(avatarCode, haircutId, request));
			return request;
		}

		/// <summary>
		/// Returns identities of all created avatars. Result contains array ordered by descending avatar creation date.
		/// </summary>
		public AsyncRequest<string[]> GetAllAvatarsAsync(int maxItems)
		{
			var request = new AsyncRequest<string[]>(AvatarSdkMgr.Str(Strings.GettingAvatarList));
			var allAvatars = session.GetAvatarsFromFilesystem().OrderByDescending(x => x).ToArray();
			if (allAvatars.Length > maxItems)
			{
				string[] selectedAvatars = new string[maxItems];
				Array.Copy(allAvatars, selectedAvatars, maxItems);
				request.Result = selectedAvatars;
			}
			else
				request.Result = allAvatars;
			request.IsDone = true;
			return request;
		}

		/// <summary>
		/// Deletes all avatar files from the local storage
		/// </summary>
		public AsyncRequest DeleteAvatarAsync(string avatarCode)
		{
			var request = new AsyncRequest(AvatarSdkMgr.Str(Strings.DeletingAvatarFiles));
			CoreTools.DeleteAvatarFiles(avatarCode);
			request.IsDone = true;
			return request;
		}

		/// <summary>
		/// Check if the local compute version of the SDK supports given pipeline
		/// </summary>
		public AsyncRequest<bool> IsPipelineSupportedAsync(PipelineType pipelineType)
		{
			var request = new AsyncRequest<bool>();
			request.Result = PipelineTraitsFactory.Instance.IsTypeSupported(pipelineType);
			request.IsDone = true;
			return request;
		}

		public AsyncRequest<ComputationParameters> GetParametersAsync(ComputationParametersSubset parametersSubset, PipelineType pipelineType)
		{
			return session.ComputationParametersController.GetParametersAsync(parametersSubset, pipelineType);
		}

		#endregion

		#region IDisposable
		public virtual void Dispose()
		{
			session.Dispose();
		}
#endregion

#region public methods
		/// <summary>
		/// Get the created session instance.
		/// </summary>
		public Session Session { get { return session; } }
#endregion

#region private methods

		/// <summary>
		/// InitializeAvatarAsync implementation
		/// </summary>
		private IEnumerator InitializeAvatarFunc(byte[] photoBytes, ComputationParameters computationParameters, AsyncRequest<string> request, PipelineType pipelineType)
		{
			//Decode photo to a raw buffer of bytes
			var photoTexture = new Texture2D(1, 1);
			photoTexture.LoadImage(photoBytes);
			RawPhoto rawPhoto = new RawPhoto(photoTexture.GetPixels32(), photoTexture.width, photoTexture.height);

			while (!session.IsInitialized)
			{
				Debug.LogFormat("Waiting until SDK session initializes!");
				yield return new WaitForSecondsRealtime(0.02f);
			}

			var initializeAvatar = session.InitializeAvatarAsync(rawPhoto, pipelineType, computationParameters: computationParameters);
			yield return request.AwaitSubrequest(initializeAvatar, 0.01f);
			if (request.IsError)
				yield break;

			string avatarCode = initializeAvatar.Result;

			CoreTools.SavePipelineType(pipelineType, avatarCode);

			request.Result = initializeAvatar.Result;
			request.IsDone = true;
		}

		/// <summary>
		/// StartAndAwaitAvatarCalculationAsync implementation
		/// </summary>
		private IEnumerator StartAndAwaitAvatarCalculationFunc(string avatarCode, PipelineType pipelineType, AsyncRequest request)
		{
			while (!session.IsInitialized)
			{
				Debug.LogFormat("Waiting until SDK session initializes!");
				yield return new WaitForSecondsRealtime(0.02f);
			}

			yield return request.AwaitSubrequest(session.CalculateAvatarLocalComputeAsync(avatarCode, pipelineType), 1.0f);
			Debug.LogFormat("Avatar calculated!");

			request.IsDone = true;
		}

		private IEnumerator GenerateHaircutsFunc(string avatarCode, List<string> haircutsList, AsyncRequest request)
		{
			while (!session.IsInitialized)
			{
				Debug.LogFormat("Waiting until SDK session initializes!");
				yield return new WaitForSecondsRealtime(0.02f);
			}

			yield return request.AwaitSubrequest(session.GenerateHaircutsAsync(avatarCode, haircutsList), 1.0f);
			Debug.LogFormat("Haircuts are generated!");

			request.IsDone = true;
		}

		/// <summary>
		/// GetHaircutsIdAsync implementation
		/// </summary>
		private IEnumerator GetHaircutsIdFunc(string avatarCode, AsyncRequest<string[]> request)
		{
			//var haircutsWithFilenames = AvatarSdkMgr.Storage().GetAvatarHaircutsFilenames(avatarCode);
			var haircutsWithFilenames = HaircutsPersistentStorage.Instance.ReadHaircutsMetadataFromFile(avatarCode);
			request.Result = haircutsWithFilenames.Select(hc => hc.FullId).ToArray();
			request.IsDone = true;
			yield return request;
		}

		/// <summary>
		/// GetHaircutMeshAsync implementation
		/// </summary>
		private IEnumerator GetHaircutMeshFunc(string avatarCode, string haircutId, AsyncRequest<TexturedMesh> request)
		{
			// If haircut files are not found, let's unpack them from resources. Technically, files might exist
			// on disk but be corrupted, e.g. if app has been shut down in the middle of extract. Production app
			// would do something about this, like change state in database when haircut is fully extracted.
			// But this is a very rare case, and this sample does not handle this for sake of simplicity.
			var metadata = HaircutsPersistentStorage.Instance.GetHaircutMetadata(haircutId, avatarCode);

			string haircutMeshFilename = metadata.MeshPly;
			string haircutTextureFilename = metadata.Texture;

			if (!File.Exists(haircutMeshFilename) || !File.Exists(haircutTextureFilename))
			{
				var extractHaircutRequest = session.ExtractHaircutAsync(metadata.FullId);
				yield return request.AwaitSubrequest(extractHaircutRequest, 0.5f);
			}

			var loadHaircutRequest = CoreTools.LoadHaircutFromDiskAsync(avatarCode, metadata.FullId);
			yield return request.AwaitSubrequest(loadHaircutRequest, 1.0f);
			if (request.IsError)
				yield break;

			request.IsDone = true;
			request.Result = loadHaircutRequest.Result;
		}

		/// <summary>
		/// GetHaircutPreviewAsync implementation
		/// </summary>
		private IEnumerator GetHaircutPreviewFunc(string avatarCode, string haircutId, AsyncRequest<byte[]> request)
		{
			string haircutPreviewFilename = HaircutsPersistentStorage.Instance.GetHaircutMetadata(haircutId, avatarCode).Preview;
			if (string.IsNullOrEmpty(haircutPreviewFilename))
			{
				request.IsDone = true;
				yield break;
			}

			// Checks is the haircut preview exist om disk. If it is not, unpack it fron resources.
			if (!File.Exists(haircutPreviewFilename))
			{
				var extractPreviewRequest = session.ExtractHaircutPreviewAsync(haircutId);
				yield return request.AwaitSubrequest(extractPreviewRequest, 0.95f);

				if (!File.Exists(haircutPreviewFilename))
				{
					Debug.LogErrorFormat("Haircut preview file not extracted: {0}", haircutPreviewFilename);
					request.SetError("Haircut preview file not found.");
					yield break;
				}
			}

			request.Result = File.ReadAllBytes(haircutPreviewFilename);
			request.IsDone = true;
		}

		/// <summary>
		/// GetHeadMeshAsync implementation
		/// </summary>
		private IEnumerator GetHeadMeshFunc(string avatarCode, bool withBlendshapes, int detailsLevel, string additionalTextureName, AsyncRequest<TexturedMesh> request)
		{
			if (detailsLevel != 0)
			{
				//Firstly we need to check if the LOD mesh is already generated.
				string meshFilename = AvatarSdkMgr.Storage().GetAvatarFilename(avatarCode, AvatarFile.MESH_PLY, detailsLevel);
				List<string> blendshapesDirs = AvatarSdkMgr.Storage().GetAvatarBlendshapesDirs(avatarCode, detailsLevel);
				bool blendshapesExist = true;
				foreach (string dir in blendshapesDirs)
				{
					if (!Directory.Exists(dir) || Directory.GetFiles(dir).Length == 0)
						blendshapesExist = false;
				}

				if(!blendshapesExist || !File.Exists(meshFilename))
				{
					var generateLODRequest = session.GenerateLODMeshAsync(avatarCode, detailsLevel);
					yield return request.AwaitSubrequest(generateLODRequest, 0.9f);
					if (request.IsError)
						yield break;
				}
			}

			var loadAvatarHeadRequest = CoreTools.LoadAvatarHeadFromDiskAsync(avatarCode, withBlendshapes, detailsLevel, additionalTextureName);
			yield return request.AwaitSubrequest(loadAvatarHeadRequest, 1.0f);
			if (request.IsError)
				yield break;

			request.Result = loadAvatarHeadRequest.Result;
			request.IsDone = true;
		}

		private IEnumerator GetTextureFunc(string avatarCode, string textureName, AsyncRequest<Texture2D> request)
		{
			var loadTextureRequest = CoreTools.LoadAvatarTextureFromDiskAsync(avatarCode, textureName);
			yield return request.AwaitSubrequest(loadTextureRequest, 1.0f);
			if (request.IsError)
				yield break;

			request.Result = loadTextureRequest.Result;
			request.IsDone = true;
		}


#endregion
	}
}
#endif
