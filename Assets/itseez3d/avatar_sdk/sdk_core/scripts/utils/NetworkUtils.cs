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
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ItSeez3D.AvatarSdk.Core
{
	/// <summary>
	/// Some basic network stuff needed in both Cloud and Local Compute SDK.
	/// </summary>
	public static class NetworkUtils
	{
		/// <summary>
		/// The root URL for Avatar SDK server.
		/// </summary>
		private const string defaultApiUrl = "https://api.avatarsdk.com";

		private const string backupApiUrl = "https://avatar-api.itseez3d.com";

		private const string apiUrlFilename = "avatar_sdk_api_url";

		private static string loadedApiUrl = string.Empty;

#if UNITY_EDITOR
		public static void StoreApiUrl(string apiUrl)
		{
			if (string.IsNullOrEmpty(apiUrl))
				return;

			string miscAuthResourcesPath = PluginStructure.GetPluginDirectoryPath(PluginStructure.MISC_AUTH_RESOURCES_DIR, PathOriginOptions.FullPath);
			PluginStructure.CreatePluginDirectory(miscAuthResourcesPath);
			var path = Path.Combine(miscAuthResourcesPath, apiUrlFilename + ".txt");

			File.WriteAllText(path, apiUrl);
			UnityEditor.AssetDatabase.SaveAssets();
			UnityEditor.AssetDatabase.Refresh();

			LoadApiUrlFromAssets();
		}
#endif

		public static string DefaultApiUrl
		{
			get { return defaultApiUrl; }
		}

		public static string BackupApiUrl
		{
			get { return backupApiUrl; }
		}

		public static string ApiUrl
		{
			get
			{
				if (string.IsNullOrEmpty(loadedApiUrl))
					LoadApiUrlFromAssets();
				return loadedApiUrl;
			}
		}

		private static void LoadApiUrlFromAssets()
		{
			loadedApiUrl = defaultApiUrl;

			try
			{
				var asset = Resources.Load(apiUrlFilename) as TextAsset;
				if (asset != null && !string.IsNullOrEmpty(asset.text))
					loadedApiUrl = asset.text;
			}
			catch (Exception ex)
			{
				Debug.LogFormat("Could not load api url: {0}", ex.Message);
			}
		}

		public static bool IsWebRequestFailed(UnityWebRequest webRequest)
		{
#if UNITY_2020_2_OR_NEWER
			return webRequest.result == UnityWebRequest.Result.ConnectionError;
#else
			return webRequest.isNetworkError;
#endif
		}
	}
}
