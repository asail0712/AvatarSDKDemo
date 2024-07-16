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

using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarSdk.LocalCompute;
using ItSeez3D.AvatarSdkSamples.Core;
using System.Collections;
using System.IO;

namespace ItSeez3D.AvatarSdkSamples.LocalCompute
{
	public class GallerySampleLocalCompute : GallerySample
	{
		public GallerySampleLocalCompute()
		{
			sdkType = SdkType.LocalCompute;
		}

		protected override AsyncRequest<GalleryAvatar[]> GetAllAvatarsAsync(int maxItems)
		{
			var request = new AsyncRequest<GalleryAvatar[]>(AvatarSdkMgr.Str(Strings.GettingAvatarState));
			AvatarSdkMgr.SpawnCoroutine(GetAllAvatarsFunc(maxItems, request));
			return request;
		}

		private IEnumerator GetAllAvatarsFunc(int maxItems, AsyncRequest<GalleryAvatar[]> request)
		{
			var avatarsRequest = avatarProvider.GetAllAvatarsAsync(maxItems);
			yield return Await(avatarsRequest, null);
			if (avatarsRequest.IsError)
				yield break;

			GalleryAvatar[] avatars = new GalleryAvatar[avatarsRequest.Result.Length];
			for (int i = 0; i < avatars.Length; i++)
			{
				string avatarCode = avatarsRequest.Result[i];
				avatars[i] = new GalleryAvatar() { code = avatarCode, state = GetAvatarState(avatarCode) };
			}

			request.Result = avatars;
			request.IsDone = true;
		}

		/// <summary>
		/// Determinates state of the avatar. It simply checks existence of the mesh file. 
		/// </summary>
		private GalleryAvatarState GetAvatarState(string avatarCode)
		{
			LocalComputeAvatarProvider localComputeAvatarProvider = avatarProvider as LocalComputeAvatarProvider;

			GalleryAvatarState avatarState = GalleryAvatarState.UNKNOWN;

			if (localComputeAvatarProvider.Session.IsAvatarCalculating(avatarCode))
				avatarState = GalleryAvatarState.GENERATING;
			else
			{
				string meshFilePath = AvatarSdkMgr.Storage().GetAvatarFilename(avatarCode, AvatarFile.MESH_PLY);
				if (File.Exists(meshFilePath))
					avatarState = GalleryAvatarState.COMPLETED;
				else
					avatarState = GalleryAvatarState.FAILED;
			}

			return avatarState;
		}
	}
}
#endif
