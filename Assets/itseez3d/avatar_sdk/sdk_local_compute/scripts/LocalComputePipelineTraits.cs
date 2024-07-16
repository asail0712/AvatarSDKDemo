/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, September 2019
*/

using System.Collections.Generic;
using System.IO;
using ItSeez3D.AvatarSdk.Core;

namespace ItSeez3D.AvatarSdk.LocalCompute.PipelineTraits
{
	public class LocalComputeTraitsKeeper : IPipelineTraitsKeeper
	{
		public PipelineType DefaultPipeline
		{
			get
			{
				return PipelineType.HEAD_2_0_HEAD_MOBILE;
			}
		}

		public Dictionary<PipelineType, PipelineTypeTraits> GetTraits()
		{
			var result = new Dictionary<PipelineType, PipelineTypeTraits>();
			result.Add(PipelineType.HEAD_2_0_BUST_MOBILE, new Bust2Traits());
			result.Add(PipelineType.HEAD_2_0_HEAD_MOBILE, new Head2Traits());
			result.Add(PipelineType.UMA_MALE, new UmaMaleTraits());
			result.Add(PipelineType.UMA_FEMALE, new UmaFemaleTraits());
			return result;
		}
	}

	public class Head2Traits : Head2AbstractTraits
	{
		public sealed override string PipelineSubtypeName { get { return "head/mobile"; } }
		public sealed override string DisplayName { get { return "Head 2.0 | head/mobile"; } }
		public sealed override PipelineType Type { get { return PipelineType.HEAD_2_0_HEAD_MOBILE; } }
	}

	public class Bust2Traits : Head2AbstractTraits
	{
		public sealed override string PipelineSubtypeName { get { return "bust/mobile"; } }
		public sealed override string DisplayName { get { return "Bust 2.0 | bust/mobile"; } }
		public sealed override PipelineType Type { get { return PipelineType.HEAD_2_0_BUST_MOBILE; } }
		 
	}

	public class UmaMaleTraits : Head2AbstractTraits
	{
		public sealed override string PipelineSubtypeName { get { return "uma2/male"; } }
		public sealed override string DisplayName { get { return "Uma Male"; } }
		public sealed override PipelineType Type { get { return PipelineType.UMA_MALE; } }
	}

	public class UmaFemaleTraits : Head2AbstractTraits
	{
		public sealed override string PipelineSubtypeName { get { return "uma2/female"; } }
		public sealed override string DisplayName { get { return "Uma Female"; } }
		public sealed override PipelineType Type { get { return PipelineType.UMA_FEMALE; } }
	}
}
