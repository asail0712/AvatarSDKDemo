/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

#if UNITY_EDITOR && !UNITY_WEBGL
using System.Collections;
using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarSdk.Core.Editor;
using UnityEditor;
using UnityEngine;
using Coroutines;

namespace ItSeez3D.AvatarSdk.LocalCompute.Editor
{
	[InitializeOnLoad]
	public static class LocalComputeSdkEditor
	{
		static LocalComputeSdkEditor ()
		{
			EditorApplication.update += InitializeOnce;
			EditorApplication.update += CheckUpdatedCredentials;
		}

		private static void InitializeOnce ()
		{
			EditorApplication.update -= InitializeOnce;

			if (Utils.IsDesignTime ())
				InitializeLocalComputeSdk (resetResources: false);
		}

		private static void CheckUpdatedCredentials ()
		{
			if (AuthenticationWindow.justUpdatedCredentials) {
				AuthenticationWindow.justUpdatedCredentials = false;
				Debug.LogFormat ("Just updated API credentials - need to reload the local compute SDK resources");

				InitializeLocalComputeSdk (resetResources: true);
			}
		}

		[MenuItem ("Window/itSeez3D Avatar SDK/Local Compute SDK/Force reset the SDK license and resources")]
		public static void UpdateLicense ()
		{
			if (Utils.IsDesignTime ())
				InitializeLocalComputeSdk (resetResources: true);
			else
				Debug.LogFormat ("Please don't use this in play mode. Disable play mode to reset the Local Compute SDK");
		}

		private static void InitializeLocalComputeSdk (bool resetResources)
		{
			if (!AvatarSdkMgr.IsInitialized)
				AvatarSdkMgr.Init (sdkType: SdkType.LocalCompute);
			var resourcesPath = AvatarSdkMgr.Storage ().GetResourcesDirectory ();
			EditorRunner.instance.Run(EnsureInitialized(resetResources));
		}

		private static IEnumerator EnsureInitialized(bool resetResources)
		{
			AsyncRequest initializeRequest = LocalComputeSdkUtils.EnsureInitializedAsync(AvatarSdkMgr.Storage().GetResourcesDirectory(), resetResources: resetResources);
			yield return initializeRequest;
		}
	}
}

#endif