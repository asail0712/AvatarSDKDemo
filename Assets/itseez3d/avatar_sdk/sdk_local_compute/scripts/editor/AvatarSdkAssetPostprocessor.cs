/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

using ItSeez3D.AvatarSdk.Core;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Coroutines;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.LocalCompute.Editor
{
	public class AvatarSdk : AssetPostprocessor
	{
		readonly static string avatarSdkResourceSuffix = "res_";
		readonly static string avatarSdkResourcePostfix = ".bytes";

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			bool isResourcesUpdated = false;

			List<string> modifiedAssets = new List<string>();
			if (importedAssets != null)
				modifiedAssets.AddRange(importedAssets);
			if (deletedAssets != null)
				modifiedAssets.AddRange(deletedAssets);
			if (movedAssets != null)
				modifiedAssets.AddRange(movedAssets);

			foreach (string asset in modifiedAssets)
			{
				string resourceName = Path.GetFileName(asset);
				if (resourceName.StartsWith(avatarSdkResourceSuffix) && resourceName.EndsWith(avatarSdkResourcePostfix))
				{
					isResourcesUpdated = true;
					break;
				}
			}

			if (isResourcesUpdated && AvatarSdkMgr.IsInitialized && !LocalComputeSdkUtils.IsInitializing)
			{
				EditorRunner.instance.Run(EnsureSdkResourcesUnpacked());
			}
		}

		static IEnumerator EnsureSdkResourcesUnpacked()
		{
			AsyncRequest request = LocalComputeSdkUtils.EnsureSdkResourcesUnpackedAsync(AvatarSdkMgr.Storage().GetResourcesDirectory());
			yield return request;
			if (request.IsError)
				Debug.LogErrorFormat("Resources unpacking error: {0}", request.ErrorMessage);
		}
	}
}
