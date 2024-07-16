/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, January 2021
*/

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.Core
{
	public enum AvatarShaderType
	{
		AvatarSdkUnlit,
		AvatarSdkLit,
		Standard
	}

	public class MaterialAdjuster
	{
		private static readonly string headPbrMaterialTemplateName = "materials/AvatarPbrMaterial";

		public static Material GetHeadMaterial(string avatarCode, AvatarShaderType shaderType)
		{
			string textureFilename = AvatarSdkMgr.Storage().GetAvatarTextureFilename(avatarCode);
			Texture2D texture = new Texture2D(0, 0);
			texture.LoadImage(File.ReadAllBytes(textureFilename));
			return GetHeadMaterial(avatarCode, shaderType);
		}

		public static Material GetHeadMaterial(string avatarCode, Texture mainTexture, AvatarShaderType shaderType)
		{
			if (shaderType == AvatarShaderType.Standard)
			{
				Material templateMaterial = Resources.Load<Material>(headPbrMaterialTemplateName);
				Material material = new Material(templateMaterial);
				material.mainTexture = mainTexture;

				string roughnessMapFilename = AvatarSdkMgr.Storage().GetAvatarFilename(avatarCode, AvatarFile.ROUGHNESS_MAP_TEXTURE);
				string metallicMapFilename = AvatarSdkMgr.Storage().GetAvatarFilename(avatarCode, AvatarFile.METALLIC_MAP_TEXTURE);
				string normalMapFilename = AvatarSdkMgr.Storage().GetAvatarFilename(avatarCode, AvatarFile.NORMAL_MAP_TEXTURE);

				int metallicWithRoughnessTextureWidth = 0;
				int metallicWithRoughnessTextureHeight = 0;

				Color32[] metallnessColors = null;
				if (File.Exists(metallicMapFilename))
				{
					Texture2D metallnessTexture = new Texture2D(0, 0);
					metallnessTexture.LoadImage(File.ReadAllBytes(metallicMapFilename));
					metallicWithRoughnessTextureWidth = metallnessTexture.width;
					metallicWithRoughnessTextureHeight = metallnessTexture.height;
					metallnessColors = metallnessTexture.GetPixels32();
					Object.DestroyImmediate(metallnessTexture);
				}

				Color32[] roughnessColors = null;
				if (File.Exists(roughnessMapFilename))
				{
					Texture2D roughnessTexture = new Texture2D(0, 0);
					roughnessTexture.LoadImage(File.ReadAllBytes(roughnessMapFilename));
					metallicWithRoughnessTextureWidth = roughnessTexture.width;
					metallicWithRoughnessTextureHeight = roughnessTexture.height;
					roughnessColors = roughnessTexture.GetPixels32();
					Object.DestroyImmediate(roughnessTexture);
				}

				if (metallicWithRoughnessTextureWidth > 0 && metallicWithRoughnessTextureHeight > 0)
				{
					Texture2D metallicWithRoughnessTexture = new Texture2D(metallicWithRoughnessTextureWidth, metallicWithRoughnessTextureHeight);
					Color32[] metallicWithRoughnessTextureColors = metallicWithRoughnessTexture.GetPixels32();
					for (int i = 0; i < metallicWithRoughnessTextureColors.Length; i++)
					{
						byte metallValue = metallnessColors == null ? (byte)0 : metallnessColors[i].r;
						metallicWithRoughnessTextureColors[i].r = metallValue;
						metallicWithRoughnessTextureColors[i].g = metallValue;
						metallicWithRoughnessTextureColors[i].b = metallValue;

						metallicWithRoughnessTextureColors[i].a = 255;
						if (roughnessColors != null)
							metallicWithRoughnessTextureColors[i].a -= roughnessColors[i].r;
					}
					metallicWithRoughnessTexture.SetPixels32(metallicWithRoughnessTextureColors);
					metallicWithRoughnessTexture.Apply(true, true);

					material.SetTexture("_MetallicGlossMap", metallicWithRoughnessTexture);

					if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
						Debug.LogWarningFormat("You are using metallic map texture in the Gamma color space. This texture works better in the Linear color space: Player Settings -> Other Settings -> Color Space");
				}

				if (File.Exists(normalMapFilename))
				{
					Texture2D normalMapTexture = new Texture2D(0, 0, TextureFormat.RGB24, true, true);
					normalMapTexture.LoadImage(File.ReadAllBytes(normalMapFilename));
					material.SetTexture("_BumpMap", normalMapTexture);
				}

				Resources.UnloadUnusedAssets();

				return material;
			}
			else
			{
				Material material = new Material(ShadersUtils.GetHeadShader(shaderType == AvatarShaderType.AvatarSdkLit));
				material.mainTexture = mainTexture;
				return material;
			}
		}

		public static Material GetHaircutMaterial(string avatarCode, string haircutName, AvatarShaderType shaderType)
		{
			var haircutMetadata = HaircutsPersistentStorage.Instance.GetHaircutMetadata(haircutName, avatarCode);
			Texture2D haircutTexture = new Texture2D(0, 0);
			haircutTexture.LoadImage(File.ReadAllBytes(haircutMetadata.Texture));
			return GetHaircutMaterial(haircutTexture, haircutName, shaderType);
		}

		public static Material GetHaircutMaterial(Texture haircutTexture, string haircutName, AvatarShaderType shaderType)
		{
			bool withLighting = shaderType != AvatarShaderType.AvatarSdkUnlit;
			Shader shader = ShadersUtils.GetHaircutShader(haircutName, withLighting);
			Material haircutMaterial = new Material(shader);
			haircutMaterial.mainTexture = haircutTexture;

			bool enableCulling = withLighting && ShadersUtils.EnableCullingForHaircut(haircutName);
			haircutMaterial.SetInt("_Cull", enableCulling ? 2 : 0);
			return haircutMaterial;
		}
	}
}
