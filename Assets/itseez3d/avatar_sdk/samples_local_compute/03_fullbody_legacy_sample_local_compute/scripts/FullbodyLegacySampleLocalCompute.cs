﻿/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, July 2020
*/

using ItSeez3D.AvatarSdkSamples.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ItSeez3D.AvatarSdkSamples.LocalCompute
{
	public class FullbodyLegacySampleLocalCompute : FullbodyLegacySample
	{
		protected override void Start()
		{
			selectedPipelineType = AvatarSdk.Core.PipelineType.HEAD_2_0_HEAD_MOBILE;
			base.Start();
		}
	}
}
