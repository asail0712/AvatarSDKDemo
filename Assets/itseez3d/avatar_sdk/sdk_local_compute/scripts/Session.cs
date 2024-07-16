/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/
using System.Collections.Generic;
using AOT;

#if !UNITY_WEBGL

using System;
using System.Collections;
using System.IO;
using System.Threading;
using ItSeez3D.AvatarSdk.Core;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.LocalCompute
{
	/// <summary>
	/// Session encapsulates all the information required to interact with the native plugin.
	/// When avatar generation is no longer needed, the session can be disposed and re-created again later.
	/// Technically, Session is not a singleton, but it is not recommended to have multiple instances of Session.
	/// You should have no more than one Session object at any time. Dispose of existing session before creating a new one.
	/// </summary>
	public class Session : IDisposable
	{
		private static AsyncRequest sessionAvatarRequest = null;

		// list of avatars that is being calculated now
		private static List<string> calculatingAvatars = new List<string>();

		private bool initialized = false;

		private AutoResetEvent avatarGenerationEvent = new AutoResetEvent(true);

		private LocalComputeComputationParametersController parametersController = new LocalComputeComputationParametersController();

		/// <summary>
		/// This counter holds a number of currently active background calculations.
		/// Dispose method will block until this counter is 0, because destroying the session
		/// while background threads are still active may cause a crash.
		/// </summary>
		private int asyncOperationsCounter = 0;

		#region Atomic counter for current number of async operations

		private int AsyncOperationsCounter { get { return asyncOperationsCounter; } }

		private int IncrementAsyncOperationsCounter()
		{
			return Interlocked.Increment(ref asyncOperationsCounter);
		}

		private int DecrementAsyncOperationsCounter()
		{
			return Interlocked.Decrement(ref asyncOperationsCounter);
		}

		#endregion

		#region Initialization

		private IEnumerator InitializationHelper(AsyncRequest r)
		{
			if (IsInitialized)
			{
				r.IsDone = true;
				yield break;
			}

			var initializationStartTime = Time.realtimeSinceStartup;
			Debug.LogFormat("Initialization...");
			var status = NativeMethods.InitAvatarSdk("unity_plugin", AvatarSdkMgr.Storage().GetResourcesDirectory());  // initialization procedure for the native DLL


			bool okay = status == 0;
			if (okay)
			{
				Debug.LogFormat("Library loading successful, status: {0}", status);
			}
			else
			{
				Debug.LogErrorFormat("Library loading failed, status: {0}", status);
				r.SetError("Could not load native library");
				yield break;
			}

			NativeMethods.SetApiUrl(NetworkUtils.ApiUrl);

			r.Progress = 0.05f;

			var initTime = Time.realtimeSinceStartup;
			var resourcesPath = AvatarSdkMgr.Storage().GetResourcesDirectory();
			AsyncRequest resourcesInitializationRequest = LocalComputeSdkUtils.EnsureInitializedAsync(resourcesPath, true);
			yield return r.AwaitSubrequest(resourcesInitializationRequest, 0.99f);
			if (r.IsError)
			{
				Debug.LogErrorFormat("Session initialization error: {0}", r.ErrorMessage);
				yield break;
			}
			Debug.LogFormat("Took {0} seconds to initialize Local Compute SDK", Time.realtimeSinceStartup - initTime);

			r.Progress = 0.99f;

			IsInitialized = okay;
			Debug.LogFormat("Initialization completed! It took {0} seconds", Time.realtimeSinceStartup - initializationStartTime);

			r.IsDone = true;
		}

		public virtual AsyncRequest InitializeAsync()
		{
			var request = new AsyncRequest(AvatarSdkMgr.Str(Strings.InitializingSession));
			AvatarSdkMgr.SpawnCoroutine(InitializationHelper(request));
			return request;
		}

		public virtual bool IsInitialized
		{
			get { return initialized; }
			private set
			{
				initialized = value;
			}
		}

		#endregion

		#region Error handling

		private unsafe void HandleError(int returnCode, string whoFailed, string msg)
		{
			if (returnCode == 0)
				return;

			if (string.IsNullOrEmpty(msg))
			{
				byte[] errorBuffer = new byte[1024];
				fixed (byte* rawBytes = &errorBuffer[0])
					NativeMethods.GetLastError(rawBytes, errorBuffer.Length);

				msg = System.Text.Encoding.ASCII.GetString(errorBuffer);
			}
			string errorMessage = string.Format("{0} failed with code: {1}, {2}", whoFailed, returnCode, msg);

			throw new Exception(errorMessage);
		}

		#endregion

		#region "Session" implementation

		private IEnumerator ReleaseResource(AsyncRequest request, TextAsset resource)
		{
			while (!request.IsDone)
				yield return null;

			Debug.LogFormat("Releasing resource...");
			Resources.UnloadAsset(resource);
			Resources.UnloadUnusedAssets();
			GC.Collect();
		}

		private unsafe int AvatarInitialization(RawPhoto rawPhoto, string avatarDirectory, PipelineType pipelineType, ComputationParameters computationParameters = null)
		{
			try
			{
				// To provide backward compatibility, use default set of parameters if corresponding paremeter is null
				if (computationParameters == null)
					computationParameters = parametersController.GetParameters(ComputationParametersSubset.DEFAULT, pipelineType);

				string json = parametersController.GetCalculationParametersJson(pipelineType, computationParameters);
				string parametersFilePath = Path.Combine(avatarDirectory, AvatarSdkMgr.Storage().AvatarFilenames[AvatarFile.PARAMETERS_JSON]);
				File.WriteAllText(parametersFilePath, json);
			}
			catch (Exception exc)
			{
				Debug.LogErrorFormat("Exception during creating file with parameters: {0}", exc);
			}

			fixed (Color32* rawBytes = &rawPhoto.rawData[0])
			{
				IntPtr rawBytesPtr = (IntPtr)rawBytes;
				return NativeMethods.InitializeAvatarFromRawData(rawBytesPtr, rawPhoto.w, rawPhoto.h, avatarDirectory);
			}
		}

		/// <summary>
		/// Creates a unique identifier that we will use to refer to the particular avatar.
		/// </summary>
		/// <returns>The local compute avatar identifier.</returns>
		/// <param name="param">User-defined parameter that can optionally be a part of id.</param>
		public virtual string GenerateLocalComputeAvatarId(string param)
		{
			var avatarId = string.Format("local_compute_avatar_{0}_{1}", DateTime.Now.ToString("yyyyMMddHHmmss"), Guid.NewGuid().ToString("N"));
			return avatarId;
		}

		/// <summary>
		/// Simply loads the list of directories containing avatars generated locally.
		/// </summary>
		public virtual string[] GetAvatarsFromFilesystem()
		{
			var avatarsDir = AvatarSdkMgr.Storage().GetAvatarsDirectory();
			var avatarDirectories = Directory.GetDirectories(avatarsDir, "local_compute_avatar_*_*", SearchOption.TopDirectoryOnly);  // this should match the GenerateLocalComputeAvatarId function
			var avatarIds = new List<string>();
			foreach (var dir in avatarDirectories)
				avatarIds.Add(Path.GetFileName(dir));
			return avatarIds.ToArray();
		}

		/// <summary>
		/// Prepare avatar directory for calculations; call this first before calling CalculateAvatarLocalComputeAsync.
		/// </summary>
		public virtual AsyncRequest<string> InitializeAvatarAsync(RawPhoto rawPhoto, PipelineType pipelineType, string yourId = "", ComputationParameters computationParameters = null)
		{
			var avatarId = GenerateLocalComputeAvatarId(yourId);
			var avatarDirectory = AvatarSdkMgr.Storage().GetAvatarDirectory(avatarId);
			var request = new AsyncRequestThreaded<string>(() =>
			{
				try
				{
					IncrementAsyncOperationsCounter();
					int returnCode = AvatarInitialization(rawPhoto, avatarDirectory, pipelineType, computationParameters);
					HandleError(returnCode, "Avatar initialization", null);
				}
				finally
				{
					DecrementAsyncOperationsCounter();
				}
				return avatarId;
			}, AvatarSdkMgr.Str(Strings.InitializingAvatar), startImmediately: false);

			if (!IsInitialized)
			{
				request.SetError("Session is not initialized yet!");
				return request;
			}

			request.StartThread();
			AvatarSdkMgr.SpawnCoroutine(request.Await());
			return request;
		}

		/// <summary>
		/// Must be static for iOS.
		/// </summary>
		[MonoPInvokeCallback(typeof(ReportProgress))]
		private static void ReportProgressForAvatar(float progress)
		{
			if (sessionAvatarRequest != null)
				sessionAvatarRequest.Progress = progress;
			else
				Debug.LogFormat("There's no active async request");
		}

		/// <summary>
		/// Start avatar generation in the native plugin.
		/// </summary>
		public virtual AsyncRequest<int> CalculateAvatarLocalComputeAsync(string avatarId, PipelineType pipelineType)
		{
			var avatarDirectory = AvatarSdkMgr.Storage().GetAvatarDirectory(avatarId);
			var request = new AsyncRequestThreaded<int>((r) =>
			{
				try
				{
					// The avatars can't be calculated simultaneously. So we need to ensure that only on avatar is generated at time.
					avatarGenerationEvent.WaitOne();

					// Waiting for an entire calculation on exit can be very annoying during development, so you can try and comment Increment/Decrement lines.
					// Be careful! If you do this, it may crash if you stop the application in a bad moment.
					// We promise to introduce the calculation interrupt mechanism in future versions.
					IncrementAsyncOperationsCounter();

					if (!NativeMethods.IsHardwareSupported())
						throw new Exception("Unable to generate avatar. Your CPU doesn't support AVX extensions set required for avatar generation");

					DateTime startTime = DateTime.Now;

					string parametersFilePath = Path.Combine(avatarDirectory, AvatarSdkMgr.Storage().AvatarFilenames[AvatarFile.PARAMETERS_JSON]);
					string inPhotoFilePath = Path.Combine(avatarDirectory, AvatarSdkMgr.Storage().AvatarFilenames[AvatarFile.PHOTO]);

					int resultCode = 0;
					if (pipelineType == PipelineType.UMA_MALE || pipelineType == PipelineType.UMA_FEMALE)
					{
						resultCode = NativeMethods.GenerateUmaAvatar(inPhotoFilePath, avatarDirectory, parametersFilePath, ReportProgressForAvatar);
					}
					else
					{
						resultCode = NativeMethods.GenerateAvatar(pipelineType.ToSdkPipelineSubType(), inPhotoFilePath, avatarDirectory, parametersFilePath, ReportProgressForAvatar);
					}

					if (resultCode != 0)
					{
						string errorMsg = string.Empty;
						if (resultCode == 13)
							errorMsg = "Unable to read source image.";
						else if (resultCode == 14)
							errorMsg = "Not enough memory.";
						else if (resultCode == 26)
							errorMsg = "Face not found.";
						HandleError(resultCode, "Calculations", errorMsg);
					}

					Debug.LogFormat("Calculation time: {0} sec", (DateTime.Now - startTime).TotalSeconds);
					return resultCode;
				}
				finally
				{
					DecrementAsyncOperationsCounter();
					avatarGenerationEvent.Set();
				}
			}, AvatarSdkMgr.Str(Strings.ComputingAvatar), startImmediately: false);

			if (!IsInitialized)
			{
				request.SetError("Session is not initialized yet!");
				return request;
			}

			sessionAvatarRequest = request;
			lock (calculatingAvatars)
				calculatingAvatars.Add(avatarId);

			request.SetOnCompleted ((r) => {
				Debug.LogFormat ("Calculations completed!");
				sessionAvatarRequest = null;
				lock (calculatingAvatars)
					calculatingAvatars.Remove(avatarId);
			});

			request.StartThread ();
			AvatarSdkMgr.SpawnCoroutine (request.Await ());
			return request;
		}

		/// <summary>
		/// Calculates haircuts for existing avatar
		/// </summary>
		public virtual AsyncRequest<int> GenerateHaircutsAsync(string avatarId, List<string> haircutsList)
		{
			var storage = AvatarSdkMgr.Storage();
			var meshFilePath = storage.GetAvatarFilename(avatarId, AvatarFile.MESH_PLY);
			var avatarDirectory = storage.GetAvatarDirectory(avatarId);

			PipelineType pipelineType = CoreTools.LoadPipelineType(avatarId);
			ComputationParameters haircutsResources = ComputationParameters.Empty;
			haircutsResources.haircuts = new ComputationList(haircutsList);
			string haircutsJson = parametersController.GetCalculationParametersJson(pipelineType, haircutsResources);

			var request = new AsyncRequestThreaded<int>(() =>
			{
				int returnCode = -1;
				try
				{
					IncrementAsyncOperationsCounter();
					returnCode = NativeMethods.GenerateHaircuts(meshFilePath, avatarDirectory, haircutsJson);
					HandleError(returnCode, "Haircuts generation", null);
				}
				catch(Exception exc)
				{
					HandleError(returnCode, "Haircuts generation", exc.Message);
				}
				finally
				{
					DecrementAsyncOperationsCounter();
				}
				return returnCode;
			}, AvatarSdkMgr.Str(Strings.InitializingAvatar), startImmediately: false);

			if (!IsInitialized)
			{
				request.SetError("Session is not initialized yet!");
				return request;
			}

			request.StartThread();
			AvatarSdkMgr.SpawnCoroutine(request.Await());
			return request;
		}

		/// <summary>
		/// Generate an avatar mesh with the given level of details.
		/// </summary>
		public virtual AsyncRequest<int> GenerateLODMeshAsync(string avatarId, int levelOfDetails)
		{
			string meshFilePath = AvatarSdkMgr.Storage().GetAvatarFilename(avatarId, AvatarFile.MESH_PLY);
			string lodMeshFilePath = AvatarSdkMgr.Storage().GetAvatarFilename(avatarId, AvatarFile.MESH_PLY, levelOfDetails);
			string blendshapesDirectory = AvatarSdkMgr.Storage().GetAvatarBlendshapesRootDir(avatarId);
			string lodBlendshapesDirectory = AvatarSdkMgr.Storage().GetAvatarBlendshapesRootDir(avatarId, levelOfDetails);

			PipelineType pipelineType = CoreTools.LoadPipelineType(avatarId);

			var request = new AsyncRequestThreaded<int>(() => {
				int code = 0;
				try
				{
					IncrementAsyncOperationsCounter();
					code = NativeMethods.GenerateLODMesh(pipelineType.ToSdkPipelineSubType(), levelOfDetails, meshFilePath, lodMeshFilePath, blendshapesDirectory, lodBlendshapesDirectory);
				}
				finally
				{
					DecrementAsyncOperationsCounter();
				}
				return code;
			}, AvatarSdkMgr.Str(Strings.GeneratingLODMesh));

			AvatarSdkMgr.SpawnCoroutine(request.Await());
			return request;
		}

		/// <summary>
		/// Unpack haircut mesh and texture from the binary resources. See the samples for details.
		/// </summary>
		public virtual AsyncRequest<string> ExtractHaircutAsync (string haircutId)
		{
			var haircutsDirectory = HaircutsPersistentStorage.Instance.GetCommonHaircutsDirectory();
			var request = new AsyncRequestThreaded<string> (() => {
				try {
					IncrementAsyncOperationsCounter ();
					NativeMethods.ExtractHaircutFromResources (haircutId, haircutsDirectory, AvatarSdkMgr.Storage().GetResourcesDirectory());
				} finally {
					DecrementAsyncOperationsCounter ();
				}
				return haircutsDirectory;
			}, AvatarSdkMgr.Str (Strings.ExtractingHaircut));

			AvatarSdkMgr.SpawnCoroutine (request.Await ());
			return request;
		}

		/// <summary>
		/// Unpack haircut preview image from the binary resources.
		/// </summary>
		/// <param name="haircutId"></param>
		/// <returns></returns>
		public virtual AsyncRequest<string> ExtractHaircutPreviewAsync(string haircutId)
		{
			var haircutsDirectory = HaircutsPersistentStorage.Instance.GetCommonHaircutsDirectory();
			var request = new AsyncRequestThreaded<string>(() => {
				try {
					IncrementAsyncOperationsCounter();
					NativeMethods.ExtractHaircutPreviewFromResources(haircutId, haircutsDirectory, AvatarSdkMgr.Storage().GetResourcesDirectory());
				}
				finally {
					DecrementAsyncOperationsCounter();
				}
				return haircutsDirectory;
			}, AvatarSdkMgr.Str(Strings.ExtractingHaircutPreview));

			AvatarSdkMgr.SpawnCoroutine(request.Await());
			return request;
		}

		/// <summary>
		/// Return True, if avatar is being calculated
		/// </summary>
		public bool IsAvatarCalculating(string avatarId)
		{
			lock (calculatingAvatars)
				return calculatingAvatars.Contains(avatarId);
		}

		public LocalComputeComputationParametersController ComputationParametersController
		{
			get { return parametersController; }
		}


		#endregion

		#region IDisposable implementation

		/// <summary>
		/// This is crucial! Dispose must be called when session is no longer needed!
		/// </summary>
		public virtual void Dispose ()
		{
			while (AsyncOperationsCounter > 0) {
				Debug.LogFormat ("Waiting for {0} unfinished async operations", AsyncOperationsCounter);
				Thread.Sleep (50);
			}

			NativeMethods.ReleaseAvatarSdk();
			IsInitialized = false;
			Debug.LogFormat ("Session Dispose completed!");
		}

#endregion
	}
}

#endif