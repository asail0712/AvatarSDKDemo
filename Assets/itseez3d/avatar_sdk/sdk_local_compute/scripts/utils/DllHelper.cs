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

namespace ItSeez3D.AvatarSdk.LocalCompute
{
	/// <summary>
	/// Holds name of the native plugin that does the avatar generation.
	/// </summary>
	public static class DllHelper
	{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		public const string dll = "avatar_sdk_2";
#elif UNITY_ANDROID
		public const string dll = "libavatar_sdk_2.so";
#elif UNITY_IOS
		public const string dll =  "__Internal";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
		public const string dll =  "avatar_sdk_2";
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
		public const string dll = "avatar_sdk";
#else
		public const string dll = "LOCAL_COMPUTE_SDK_IS_NOT_SUPPORTED_ON_THIS_PLATFORM_YET";
#endif
	}
}