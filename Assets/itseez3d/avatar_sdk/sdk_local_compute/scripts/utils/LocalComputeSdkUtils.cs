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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ItSeez3D.AvatarSdk.Core;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ItSeez3D.AvatarSdk.LocalCompute
{
	public class LocalComputeSdkUtils
	{
		private static readonly string EXTERNAL_RESOURCES_URL = "http://localhost:8000/";

		protected static readonly string localComputeResourcesDir = "bin";
		protected static readonly string resourceExtension = ".bytes";

		public static bool IsInitializing { get; private set; }

		public static AsyncRequest EnsureSdkResourcesUnpackedAsync(string unpackedResourcesPath)
		{
			AsyncRequest unpackRequest = new AsyncRequest("Verifying resources");
			AvatarSdkMgr.SpawnCoroutine(EnsureSdkResourcesUnpackedFunc(unpackedResourcesPath, unpackRequest));
			return unpackRequest;
		}

		public static AsyncRequest EnsureInitializedAsync(string unpackedResourcesPath, bool showError = false, bool resetResources = false)
		{
			AsyncRequest initializeRequest = new AsyncRequest("Resources initialization");
			AvatarSdkMgr.SpawnCoroutine(EnsureInitializedFunc(unpackedResourcesPath, showError, resetResources, initializeRequest));
			return initializeRequest;
		}

		protected static IEnumerator EnsureSdkResourcesUnpackedFunc(string unpackedResourcesPath, AsyncRequest request)
		{
			var resourcesListFilename = "resource_list.txt";
			var externalResourcesListFilename = "external_resources_list.txt";

#if UNITY_EDITOR
			// getting list of local compute sdk resources and saving it to file
			AssetDatabase.Refresh();

			string resourcesDir = PluginStructure.GetPluginDirectoryPath(PluginStructure.LOCAL_COMPUTE_RESOURCES_DIR, PathOriginOptions.FullPath);
			string miscResourcesDir = PluginStructure.GetPluginDirectoryPath(PluginStructure.MISC_LOCAL_COMPUTE_RESOURCES_DIR, PathOriginOptions.FullPath);
			string umaResourcesDir = PluginStructure.GetPluginDirectoryPath(PluginStructure.UMA_RESOURCES_DIR, PathOriginOptions.FullPath);
			string externalResourcesDir = PluginStructure.GetPluginDirectoryPath(PluginStructure.LOCAL_COMPUTE_EXTERNAL_RESOURCES_DIR, PathOriginOptions.FullPath);

			PluginStructure.CreatePluginDirectory(miscResourcesDir);

			CreateExistedResourcesFilesList(Path.Combine(miscResourcesDir, resourcesListFilename), resourcesDir, miscResourcesDir, umaResourcesDir);

			// getting list of external resources and saving it to file
			CreateExistedResourcesFilesList(Path.Combine(miscResourcesDir, externalResourcesListFilename), externalResourcesDir);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
#endif

			// verify the integrity of the unpacked resource folder
			bool shouldUnpackResources = false;
			var testFile = IOUtils.CombinePaths(unpackedResourcesPath, CoreTools.LocalComputeSdkVersion.ToString() + "_copied.test");

			// first of all, check if the indicator file is present in the directory
			if (!File.Exists(testFile))
			{
				Debug.Log("Indicator file doesn't exist");
				shouldUnpackResources = true;
			}

			// get actual list of the unpacked resources
			var unpackedResources = new HashSet<string>();
			foreach (var unpackedResourcePath in Directory.GetFiles(unpackedResourcesPath, "*.*", SearchOption.AllDirectories))
			{
				var unpackedResource = Path.GetFileName(unpackedResourcePath);
				unpackedResources.Add(unpackedResource);
			}

			List<string> resourcesList = new List<string>();
			yield return ReadResourcesList(resourcesListFilename, resourcesList);
			bool unpackedAllResources = resourcesList.All(resource => unpackedResources.Contains(IOUtils.GetFileName(resource)));
			if (!unpackedAllResources)
			{
				Debug.Log("Not all resources are in local storage.");
				shouldUnpackResources = true;
			}

			List<string> externalResourcesList = new List<string>();
			yield return ReadResourcesList(externalResourcesListFilename, externalResourcesList);
			bool existAllExternalResources = externalResourcesList.All(resource => unpackedResources.Contains(IOUtils.GetFileName(resource)));
			if (!existAllExternalResources)
			{
				Debug.Log("Not all external resources are in local storage.");
				shouldUnpackResources = true;
			}

			if (shouldUnpackResources)
			{
				int totalResources = 0;
				totalResources += resourcesList.Count;
				totalResources += externalResourcesList.Count;

				IOUtils.CleanDirectory(unpackedResourcesPath);
				var loadingStartTime = Time.realtimeSinceStartup;
				Debug.LogFormat("Should unpack all resources");
				AsyncRequest unpackRequest = UnpackResourcesAsync(resourcesList.ToArray(), unpackedResourcesPath);
				yield return request.AwaitSubrequest(unpackRequest, request.Progress + (1.0f - request.Progress) * resourcesList.Count / totalResources);
				if (request.IsError)
					yield break;
				Debug.LogFormat("Took {0} seconds to unpack resources", Time.realtimeSinceStartup - loadingStartTime);

				if (externalResourcesList.Count > 0)
				{
					var downloadingStartTime = Time.realtimeSinceStartup;
					Debug.LogFormat("Should download external resources");
					AsyncRequest downloadRequest = DownloadExternalResourcesAsync(externalResourcesList.ToArray(), unpackedResourcesPath);
					yield return request.AwaitSubrequest(downloadRequest, 1.0f);
					if (request.IsError)
						yield break;
					Debug.LogFormat("Took {0} seconds to download external resources", Time.realtimeSinceStartup - downloadingStartTime);
				}
			}

			Debug.LogFormat("Unpacked and downloaded all resources!");
			if (!string.IsNullOrEmpty(testFile))
				File.WriteAllText(testFile, "unpacked!");

			request.Progress = 1.0f;
			request.IsDone = true;
		}

		protected static IEnumerator EnsureInitializedFunc(string unpackedResourcesPath, bool showError, bool resetResources, AsyncRequest request)
		{
			IsInitializing = true;

			if (resetResources)
			{
				IOUtils.CleanDirectory(unpackedResourcesPath);
#if UNITY_EDITOR
				string miscResourcesDir = PluginStructure.GetPluginDirectoryPath(PluginStructure.MISC_LOCAL_COMPUTE_RESOURCES_DIR, PathOriginOptions.RelativeToAssetsFolder);
				AssetDatabase.DeleteAsset(miscResourcesDir);
				PluginStructure.CreatePluginDirectory(miscResourcesDir);
				AssetDatabase.Refresh();
#endif
			}

			string clientId = null, clientSecret = null;
			var accessCredentials = AuthUtils.LoadCredentials();
			if (accessCredentials != null)
			{
				clientId = accessCredentials.clientId;
				clientSecret = accessCredentials.clientSecret;
			}

			var localComputeSdkInitializer = new LocalComputeSdkInitializer();
			string miscResourcesPath = PluginStructure.GetPluginDirectoryPath(PluginStructure.MISC_LOCAL_COMPUTE_RESOURCES_DIR, PathOriginOptions.FullPath);
			yield return localComputeSdkInitializer.Run(miscResourcesPath, CoreTools.LocalComputeSdkVersion.ToString(), NetworkUtils.ApiUrl, clientId, clientSecret);
			if (!localComputeSdkInitializer.Success)
			{
				if (showError)
					Utils.DisplayWarning("Could not initialize local compute SDK!", "Error message: \n" + localComputeSdkInitializer.LastError);
				request.SetError(string.Format("Could not initialize local compute SDK!", "Error message: {0}", localComputeSdkInitializer.LastError));
				yield break;
			}

			request.Progress = 0.05f;

			yield return EnsureSdkResourcesUnpackedFunc(unpackedResourcesPath, request);

			IsInitializing = false;
		}

		/// <summary>
		/// Build paths where resource should be placed. Resources from head_resources go to head_resources, 
		/// resources from face_resources go to face_resources, resources from root go to both directories
		/// </summary>
		/// <param name="srcResource">Relative path to source resource file</param>
		/// <returns>Paths where resource should be placed</returns>
		protected static IEnumerable<string> BuildDestinationResourcePath(string srcResource)
		{
			var partsOfPath = srcResource.Split(new char[] { '/', '\\' });
			var rootDir = partsOfPath.FirstOrDefault();
			var fileName = partsOfPath.LastOrDefault();
			if(partsOfPath.Length == 1)
			{
				yield return fileName;
			} else
			{
				yield return Path.Combine(rootDir, fileName);
			}
		}

		/// <summary>
		/// Copy resource to the places where it should be. 
		/// </summary>
		/// <param name="srcResoursePath">Relative path to resource file</param>
		/// <param name="dstResourceDirPath">Path to directory where resources are placed</param>
		/// <param name="bytes">Data read from resource file</param>
		/// <param name="copyFunc">Function that executes copying and accompanying operations</param>
		protected static void CopyPipelineResources(string srcResoursePath, string dstResourceDirPath, byte[] bytes, Action<string, byte[]> copyFunc)
		{
			Debug.LogFormat("Unpacking {0}...", srcResoursePath);
			copyFunc(Path.Combine(dstResourceDirPath, IOUtils.GetFileName(srcResoursePath)), bytes);
		}

		protected static string GetResourcePathInAssets(string resource)
		{
			string path = localComputeResourcesDir + "/" + Path.Combine(Path.GetDirectoryName(resource), Path.GetFileNameWithoutExtension(resource));
			return path;
		}

		protected static AsyncRequest UnpackResourcesAsync(string[] resourceList, string unpackedResourcesPath)
		{
			AsyncRequest unpackRequest = new AsyncRequest("Unpacking resources");
			AvatarSdkMgr.SpawnCoroutine(UnpackResourcesFunc(resourceList, unpackedResourcesPath, unpackRequest));
			return unpackRequest;
		}
 
		protected static IEnumerator UnpackResourcesFunc (string[] resourceList, string unpackedResourcesPath, AsyncRequest unpackRequest)
		{
			if (Utils.IsDesignTime ())
			{
				// Resources.LoadAsync does not work in Editor in Unity 5.6, hence the special non-async version
				for (int i=0; i<resourceList.Length; i++)
				{
					var resourceObject = Resources.Load (GetResourcePathInAssets(resourceList[i]));
					var asset = resourceObject as TextAsset;

					CopyPipelineResources(resourceList[i], unpackedResourcesPath, asset.bytes, 
						(string path, byte[] data) => {
							File.WriteAllBytes(path, data);
						});

					unpackRequest.Progress = (i + 1) / (float)resourceList.Length;

					yield return null;  // avoid blocking the main thread
				}
			}
			else
			{
				// load several resources at a time to reduce loading time
				int nSimultaneously = 20;
				for (int i = 0; i < resourceList.Length; i += nSimultaneously)
				{
					var resourceRequestsAsync = new Dictionary<string, ResourceRequest>();
					for (int j = 0; j < nSimultaneously && i + j < resourceList.Length; ++j)
					{
						var resource = resourceList [i + j];
						var request = Resources.LoadAsync(GetResourcePathInAssets(resource));
						resourceRequestsAsync.Add(resource, request);
					}

					yield return AsyncUtils.AwaitAll(resourceRequestsAsync.Values.ToArray());

					var copyRequests = new List<AsyncRequestThreaded<string>> ();
					foreach (var request in resourceRequestsAsync)
					{
						var asset = request.Value.asset as TextAsset;
						if (asset == null)
						{
							string errorMessage = "Asset is null! Could not unpack one of the resources!";
							Debug.LogError(errorMessage);
							unpackRequest.SetError(errorMessage);
							yield break;
						}
						else
						{
							var assetBytes = asset.bytes;
							CopyPipelineResources(request.Key, unpackedResourcesPath, assetBytes, 
								(string path, byte[] data) => {
									copyRequests.Add(new AsyncRequestThreaded<string>(() => {
										File.WriteAllBytes(path, assetBytes);
										return path;
									}));
								});
						}
					}

					yield return AsyncUtils.AwaitAll (copyRequests.ToArray ());

					foreach (var request in resourceRequestsAsync) {
						var asset = request.Value.asset as TextAsset;
						if (asset != null)
							Resources.UnloadAsset (asset);
					}

					unpackRequest.Progress = (i + 1) / (float)resourceList.Length;
				}
			}

			Resources.UnloadUnusedAssets ();
			GC.Collect ();
			unpackRequest.IsDone = true;
		}

		protected static AsyncRequest DownloadExternalResourcesAsync(string[] resourceList, string unpackedResourcesPath)
		{
			AsyncRequest downloadRequest = new AsyncRequest("Downloading resources");
			AvatarSdkMgr.SpawnCoroutine(DownloadExternalResourcesFunc(resourceList, unpackedResourcesPath, downloadRequest));
			return downloadRequest;
		}

		protected static IEnumerator DownloadExternalResourcesFunc(string[] resourceList, string unpackedResourcesPath, AsyncRequest request)
		{
			int nSimultaneously = 20;
			for (int i = 0; i < resourceList.Length; i += nSimultaneously)
			{
				Dictionary<string, UnityWebRequest> webRequests = new Dictionary<string, UnityWebRequest>();
				for (int j = 0; j < nSimultaneously && i + j < resourceList.Length; ++j)
				{
					var resource = resourceList[i + j];
					UnityWebRequest downloadRequest = UnityWebRequest.Get(EXTERNAL_RESOURCES_URL + resource);
					downloadRequest.SendWebRequest();
					webRequests.Add(resource, downloadRequest);
				}

				yield return AsyncUtils.AwaitAll(webRequests.Values.ToArray());

				foreach (var webRequestPair in webRequests)
				{
					if (NetworkUtils.IsWebRequestFailed(webRequestPair.Value))
					{
						Debug.LogErrorFormat("Unable to download resources: {0}", webRequestPair.Value.error);
						request.SetError(webRequestPair.Value.error);
						yield break;
					}
					File.WriteAllBytes(Path.Combine(unpackedResourcesPath, IOUtils.GetFileName(webRequestPair.Key)), webRequestPair.Value.downloadHandler.data);
				}

				request.Progress = (i + 1) / (float)resourceList.Length;
				Debug.LogFormat("Downloading resources... {0}%", (int)request.ProgressPercent);
			}

			request.IsDone = true;
		}

		/// <summary>
		/// Finds files in directory and adds their names to the list.
		/// </summary>
		/// <param name="fileNames">Output list with filenames</param>
		/// <param name="dir">Directory where files will be searched</param>
		/// <param name="relativeDir">Relative directory to concatenate with filename</param>
		/// <param name="includeSubDir">True if need to look up files in subdirs</param>
		/// <param name="extension">Include only files with the given extension</param>
		protected static void GetFileNamesInDirectory(ref List<string> fileNames, string dir, string relativeDir, bool includeSubDir = true, string extension = "")
		{
			if (!Directory.Exists(dir))
				return;

			foreach (var filePath in Directory.GetFiles(dir))
			{
				var filename = IOUtils.GetFileName(filePath);
				if (filename.EndsWith(extension))
					fileNames.Add(Path.Combine(relativeDir, filename));
			}

			if (includeSubDir)
			{
				foreach (var subdir in Directory.GetDirectories(dir))
					GetFileNamesInDirectory(ref fileNames, subdir, Path.Combine(relativeDir, IOUtils.GetFileName(subdir)), true, extension);
			}
		}

		protected static void CreateExistedResourcesFilesList(string listFilename, params string[] dirs)
		{
			var resourceFiles = new List<string>();
			foreach (var dir in dirs)
			{
				GetFileNamesInDirectory(ref resourceFiles, dir, "", true, resourceExtension);
			}
			File.WriteAllLines(listFilename, resourceFiles.ToArray());
		}

		protected static IEnumerator ReadResourcesList(string resourcesListFilename, List<string> resourcesNames)
		{
			// read list of all resources files
			UnityEngine.Object resourceListObject = null;
			var resourceListPath = GetResourcePathInAssets(resourcesListFilename);
			if (Utils.IsDesignTime())
				resourceListObject = Resources.Load(resourceListPath);
			else
			{
				var resourceListRequest = Resources.LoadAsync(resourceListPath);
				yield return resourceListRequest;
				resourceListObject = resourceListRequest.asset;
			}

			var resourceListAsset = resourceListObject as TextAsset;
			if (resourceListAsset == null)
			{
				Debug.LogErrorFormat("Could not read the list of resources from {0}", resourcesListFilename);
				yield break;
			}

			resourcesNames.AddRange(resourceListAsset.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
		}
	}
}
