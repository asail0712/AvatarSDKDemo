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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using ItSeez3D.AvatarSdkSamples.SamplePipelineTraits;

namespace ItSeez3D.AvatarSdkSamples.Core
{
	public class FullbodyLegacySample : GettingStartedSample
	{
		public BodyAttachment[] bodyAttachments;

		public GameObject body;

		public GameObject positionPanel;

		protected override void Start()
		{
			base.Start();

			var headPositionManager = gameObject.GetComponentInChildren<HeadPositionManager>();
			headPositionManager.PositionChanged += (Dictionary<PositionType, PositionControl> controls) => {
				foreach (var bodyAttachment in bodyAttachments)
					if(selectedPipelineType == bodyAttachment.pipelineType)
					{
						bodyAttachment.ChangePosition(controls);
					}
			};
		}

		protected override IEnumerator ConfigureComputationParameters(PipelineType pipelineType, ComputationParameters computationParameters)
		{
			var parametersRequest = avatarProvider.GetParametersAsync(ComputationParametersSubset.DEFAULT, pipelineType);
			yield return Await(parametersRequest);
			computationParameters.haircuts = parametersRequest.Result.haircuts;
			computationParameters.avatarModifications = parametersRequest.Result.avatarModifications;
			computationParameters.avatarModifications.allowModifyNeck.Value = false; //constant neck for fullbody attachment
		}

		protected override void DisplayHead(TexturedMesh headMesh, TexturedMesh haircutMesh)
		{
			if (!selectedPipelineType.SampleTraits().isCompatibleWithFullBody)
			{
				Debug.LogErrorFormat("Avatar from the {0} can't be used in Fullbody sample!", selectedPipelineType);
				return;
			}

			// create parent avatar object in a scene, attach a script to it to allow rotation by mouse
			var avatarObject = new GameObject("ItSeez3D Avatar");

			// create head object in the scene
			{
				Debug.LogFormat("Generating Unity mesh object for head...");
				var meshObject = new GameObject(HEAD_OBJECT_NAME);
				var meshRenderer = meshObject.AddComponent<SkinnedMeshRenderer>();
				meshRenderer.sharedMesh = headMesh.mesh;
				var material = new Material(ShadersUtils.GetHeadShader(false));
				material.mainTexture = headMesh.texture;
				meshRenderer.material = material;
				meshObject.transform.SetParent(avatarObject.transform);
			}

			// create haircut object in the scene
			if(haircutMesh != null)
			{
				var meshObject = new GameObject(HAIRCUT_OBJECT_NAME);
				var meshRenderer = meshObject.AddComponent<SkinnedMeshRenderer>();
				meshRenderer.sharedMesh = haircutMesh.mesh;
				meshRenderer.material = MaterialAdjuster.GetHaircutMaterial(haircutMesh.texture, currentHaircutId, AvatarShaderType.AvatarSdkUnlit);
				meshObject.transform.SetParent(avatarObject.transform);
			}

			if (bodyAttachments == null || bodyAttachments.Length <= 0)
			{
				Debug.LogError("No body attachments specified!");
				return;
			}

			foreach (var bodyAttachment in bodyAttachments)
			{
				if (selectedPipelineType == bodyAttachment.pipelineType)
				{
					GameObject copiedAvatarObject = GameObject.Instantiate(avatarObject);
					copiedAvatarObject.name = avatarObject.name;
					bodyAttachment.AttachHeadToBody(copiedAvatarObject, HEAD_OBJECT_NAME);
				}
			}

			GameObject.Destroy(avatarObject);
		}

		protected override void SetControlsInteractable(bool interactable)
		{
			base.SetControlsInteractable(interactable);
			foreach (var c in positionPanel.GetComponentsInChildren<Selectable>())
				c.interactable = interactable;
		}

		protected override IEnumerator CheckAvailablePipelines()
		{
			//Fullbody sample doesn't use Head pipeline. 
			//So we don't have to check if this pipeline available.
			yield break;
		}
	}
}
