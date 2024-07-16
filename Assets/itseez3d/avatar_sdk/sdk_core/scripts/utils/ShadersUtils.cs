/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.Core
{
	public static class ShadersUtils
	{
		public static readonly string avatarUnlitShaderName = "Avatar SDK/AvatarUnlitShader";
		public static readonly string avatarLitShaderName = "Avatar SDK/AvatarLitShader";
		public static readonly string haircutSolidUnlitShaderName = "Avatar SDK/HaircutSolidUnlitShader";
		public static readonly string haircutSolidLitShaderName = "Avatar SDK/HaircutSolidLitShader";
		public static readonly string haircutStrandUnlitShaderName = "Avatar SDK/HaircutStrandsUnlitShader";
		public static readonly string haircutStrandLitShaderName = "Avatar SDK/HaircutStrandsLitShader";

		private static List<string> haircutsForSolidShader = new List<string>()
		{
			"generated",
			"base/generated",
			"base/short_simple",
			"plus/balding",
			"plus/short_slick",
			"base/male_makehuman_short02",
			"facegen/facegen_balding",
			"plus/bob_parted"
		};

		private static List<string> haircutsWithCulling = new List<string>()
		{
			"base/female_NewSea_J096f",
			"base/female_NewSea_J086f",
			"base/female_NewSea_J123f",
			"base/male_NewSea_J082m",
			"base/ponytail_with_bangs",
			"base/long_disheveled",
			"base/long_wavy",
			"base/short_disheveled"
		};


		public static Shader GetHeadShader(bool withLighting)
		{
			string shaderName = withLighting ? avatarLitShaderName : avatarUnlitShaderName;
			Shader shader = Shader.Find(shaderName);
			if (shader == null)
				Debug.LogErrorFormat("Shader {0} isn't found", shaderName);
			return shader;
		}

		public static Shader GetHaircutShader(string haircutName, bool withLighting)
		{
			string shaderName = string.Empty;
			bool solidShaderRequired = IsSolidShaderRequiredForHaircut(haircutName);
			if (solidShaderRequired)
				shaderName = withLighting ? haircutSolidLitShaderName : haircutSolidUnlitShaderName;
			else
				shaderName = withLighting ? haircutStrandLitShaderName : haircutStrandUnlitShaderName;

			Shader shader = Shader.Find(shaderName);
			if (shader == null)
				Debug.LogErrorFormat("Shader {0} isn't found", shaderName);

			return shader;
		}

		public static bool IsSolidShaderRequiredForHaircut(string haircutName)
		{
			return haircutsForSolidShader.Contains(haircutName);
		}

		public static bool EnableCullingForHaircut(string haircutName)
		{
			return haircutsWithCulling.Contains(haircutName);
		}
	}
}
