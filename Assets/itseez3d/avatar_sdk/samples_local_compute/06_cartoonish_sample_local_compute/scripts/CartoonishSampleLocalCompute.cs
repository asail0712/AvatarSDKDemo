/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, August 2020
*/

using ItSeez3D.AvatarSdk.Core;
using System.Collections;
using ItSeez3D.AvatarSdkSamples.Core;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.IO;

namespace ItSeez3D.AvatarSdkSamples.LocalCompute
{
	public class CartoonishSampleLocalCompute : GettingStartedSample
	{
		public GameObject cartoonishLevel = null;

		public Button convertToObjButton = null;
		public Button convertToFbxButton = null;
		public Button createPrefabButton = null;


		protected readonly string GENERATED_HAIRCUT_NAME = "base/generated";

		private float cartoonishValue = 0.5f;

		private ModelExporter modelExporter = null;

		public CartoonishSampleLocalCompute()
		{
			currentHaircutId = GENERATED_HAIRCUT_NAME;
		}

		protected override IEnumerator ConfigureComputationParameters(PipelineType pipelineType, ComputationParameters computationParameters)
		{
			var parametersRequest = avatarProvider.GetParametersAsync(ComputationParametersSubset.ALL, pipelineType);
			yield return Await(parametersRequest);
			
			// Generate all available blendshapes sets
			computationParameters.blendshapes = parametersRequest.Result.blendshapes;

			// Generated haircut
			computationParameters.haircuts.AddValue(GENERATED_HAIRCUT_NAME);

			// Parameters to make cartoonish effect
			computationParameters.avatarModifications = new AvatarModificationsGroup();
			computationParameters.avatarModifications.slightlyCartoonishTexture.Value = true;
			computationParameters.avatarModifications.parametricEyesTexture.Value = true;
			computationParameters.avatarModifications.allowModifyNeck.Value = true;
			computationParameters.avatarModifications.addEyelidShadow.Value = true;
			computationParameters.avatarModifications.addGlare.Value = true;
			computationParameters.avatarModifications.enhanceLighting.Value = true;
			computationParameters.avatarModifications.removeGlasses.Value = true;
			computationParameters.avatarModifications.removeSmile.Value = true;

			// Shape modification is available only for the head/mobile subtype
			if (pipelineType == PipelineType.HEAD_2_0_HEAD_MOBILE)
			{
				computationParameters.shapeModifications = new ShapeModificationsGroup();
				computationParameters.shapeModifications.cartoonishV1.Value = cartoonishValue;
			}
		}

		/// <summary>
		/// To make Getting Started sample as simple as possible all code required for creating and
		/// displaying an avatar is placed here in a single function. This function is also a good example of how to
		/// chain asynchronous requests, just like in traditional sequential code.
		/// </summary>
		protected override IEnumerator GenerateAndDisplayHead(byte[] photoBytes, PipelineType pipeline)
		{
			generatedAvatarPipeline = pipeline;

			ComputationParameters computationParameters = ComputationParameters.Empty;
			yield return ConfigureComputationParameters(pipeline, computationParameters);

			// generate avatar from the photo and get its code in the Result of request
			var initializeRequest = avatarProvider.InitializeAvatarAsync(photoBytes, "name", "description", pipeline, computationParameters);
			yield return Await(initializeRequest);
			currentAvatarCode = initializeRequest.Result;

			StartCoroutine(SampleUtils.DisplayPhotoPreview(currentAvatarCode, photoPreview));

			var calculateRequest = avatarProvider.StartAndAwaitAvatarCalculationAsync(currentAvatarCode);
			yield return Await(calculateRequest);

			// get head mesh
			var avatarHeadRequest = avatarProvider.GetHeadMeshAsync(currentAvatarCode, true);
			yield return Await(avatarHeadRequest);
			TexturedMesh headTexturedMesh = avatarHeadRequest.Result;

			// get generated haircut mesh if exists
			TexturedMesh haircutTexturedMesh = null;
			var haircutRequest = avatarProvider.GetHaircutMeshAsync(currentAvatarCode, GENERATED_HAIRCUT_NAME);
			yield return haircutRequest;
			if (!haircutRequest.IsError)
				haircutTexturedMesh = haircutRequest.Result;

			DisplayHead(headTexturedMesh, haircutTexturedMesh);

			var avatarObject = GameObject.Find(AVATAR_OBJECT_NAME);
			modelExporter = avatarObject.AddComponent<ModelExporter>();

			if (MeshConverter.IsExportAvailable)
				convertToObjButton.gameObject.SetActive(true);

			if (MeshConverter.IsFbxFormatSupported)
				convertToFbxButton.gameObject.SetActive(true);

#if UNITY_EDITOR_WIN
			if (MeshConverter.IsExportAvailable)
				createPrefabButton.gameObject.SetActive(true);
#endif
		}

		public override void OnPipelineTypeToggleChanged(PipelineType newType)
		{
			selectedPipelineType = newType;
			cartoonishLevel.SetActive(selectedPipelineType == PipelineType.HEAD_2_0_HEAD_MOBILE);
		}

		#region UI Handling
		public void OnCartoonishSliderChanged(float val)
		{
			cartoonishValue = val;
		}

		public void ConvertAvatarToObjFormat()
		{
			var outputDir = AvatarSdkMgr.Storage().GetAvatarSubdirectory(currentAvatarCode, AvatarSubdirectory.OBJ_EXPORT);
			modelExporter.meshFormat = MeshFileFormat.OBJ;
			modelExporter.ExportModel(outputDir);

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			try
			{
				System.Diagnostics.Process.Start(outputDir);
			}
			catch (Exception exc)
			{
				Debug.LogErrorFormat("Unable to show output folder. Exception: {0}", exc);
			}
#endif
		}

		public void ExportAvatarAsFbx()
		{
			var outputFbxDir = AvatarSdkMgr.Storage().GetAvatarSubdirectory(currentAvatarCode, AvatarSubdirectory.FBX_EXPORT);
			modelExporter.meshFormat = MeshFileFormat.FBX;
			modelExporter.ExportModel(outputFbxDir);

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
			try
			{
				System.Diagnostics.Process.Start(outputFbxDir);
			}
			catch (Exception exc)
			{
				Debug.LogErrorFormat("Unable to show output folder. Exception: {0}", exc);
			}
#endif
			progressText.text = "FBX model was saved in avatar's directory.";
			Debug.LogFormat("FBX model was saved in {0}", outputFbxDir);
		}

		public void CreateAvatarPrefab()
		{
#if UNITY_EDITOR
			string prefabDir = Path.Combine(PluginStructure.GetPluginDirectoryPath(PluginStructure.PREFABS_DIR, PathOriginOptions.RelativeToAssetsFolder), currentAvatarCode);
			AvatarPrefabBuilder.CreateAvatarPrefab(prefabDir, GameObject.Find(AVATAR_OBJECT_NAME));
#endif
		}
		#endregion
	}
}
