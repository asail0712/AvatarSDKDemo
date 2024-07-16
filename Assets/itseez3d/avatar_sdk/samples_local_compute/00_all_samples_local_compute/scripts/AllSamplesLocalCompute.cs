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
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItSeez3D.AvatarSdkSamples.LocalCompute
{
	public class AllSamplesLocalCompute : MonoBehaviour
	{
		public void RunGettingStartedSample()
		{
			SceneManager.LoadScene(PluginStructure.GetScenePath(SampleScene.LOCAL_COMPUTE_01_GETTING_STARTED));
		}

		public void RunGallerySample()
		{
			SceneManager.LoadScene(PluginStructure.GetScenePath(SampleScene.LOCAL_COMPUTE_02_GALLERY));
		}

		public void RunFullbodySample()
		{
			SceneManager.LoadScene(PluginStructure.GetScenePath(SampleScene.LOCAL_COMPUTE_03_FULLBODY_LEGACY));
		}

		public void RunLODSample()
		{
			SceneManager.LoadScene(PluginStructure.GetScenePath(SampleScene.LOCAL_COMPUTE_04_LOD));
		}

		public void RunParametersSample()
		{
			SceneManager.LoadScene(PluginStructure.GetScenePath(SampleScene.LOCAL_COMPUTE_05_PARAMETERS));
		}

		public void RunCartoonishSample()
		{
			SceneManager.LoadScene(PluginStructure.GetScenePath(SampleScene.LOCAL_COMPUTE_06_CARTOONISH));
		}
	}
}
