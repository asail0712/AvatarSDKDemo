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
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.LocalCompute
{

	/// <summary>
	/// Resource manager for Local Compute SDK
	/// </summary>
	public class LocalComputeComputationParametersController : ComputationParametersController
	{
		#region Dll interface
		[DllImport(DllHelper.dll)]
		private unsafe static extern int getParametersJson(AvatarSdkPipelineSubtype subtype, byte* parametersJson, int parametersBufferSize);

		[DllImport(DllHelper.dll)]
		private unsafe static extern int getDefaultParametersJson(AvatarSdkPipelineSubtype subtype, byte* parametersJson, int parametersBufferSize);

		[DllImport(DllHelper.dll)]
		private unsafe static extern int getCustomParametersJson(AvatarSdkPipelineSubtype subtype, byte* parametersJson, int parametersBufferSize);

		[DllImport(DllHelper.dll)]
		private unsafe static extern int getUmaParametersJson(byte* parametersJson, int parametersBufferSize);
		#endregion

		/// <summary>
		/// There are three sets of the local compute parameters
		/// </summary>
		private enum LocalComputeParametersSubset
		{
			// parameters available for all users
			Base, 
			// default parameters
			Default,
			// individual parameters
			Custom
		}
		
		private Dictionary<string, ComputationParameters> computationParametersDictionary = new Dictionary<string, ComputationParameters>();
		
		
		/// <summary>
		/// Returns lists of parameters asynchronous
		/// </summary>
		public AsyncRequest<ComputationParameters> GetParametersAsync(ComputationParametersSubset parametersSubset, PipelineType pipelineType)
		{
			var request = new AsyncRequestThreaded<ComputationParameters>(() => { return GetParameters(parametersSubset, pipelineType); }, AvatarSdkMgr.Str(Strings.GettingParametersList));
			AvatarSdkMgr.SpawnCoroutine(request.Await());
			return request;
		}

		/// <summary>
		/// Converts ComputationParameters to the json format for local compute computations
		/// </summary>
		public override string GetCalculationParametersJson(PipelineType pipelineType, ComputationParameters computationParams)
		{
			JSONObject resourcesJson = new JSONObject();

			var traits = pipelineType.Traits();
			resourcesJson[PIPELINE_KEY] = traits.PipelineTypeName;
			resourcesJson[PIPELINE_SUBTYPE_KEY] = traits.PipelineSubtypeName;

			if (computationParams != null)
			{
				if (!IsListNullOrEmpty(computationParams.blendshapes.Values))
					resourcesJson[BLENDSHAPES_KEY] = computationParams.blendshapes.ToJson();

				if (!IsListNullOrEmpty(computationParams.haircuts.Values))
					resourcesJson[HAIRCUTS_KEY] = computationParams.haircuts.ToJson();

				if (!IsListNullOrEmpty(computationParams.additionalTextures.Values))
					resourcesJson[ADDITIONAL_TEXTURES] = computationParams.additionalTextures.ToJson(false);

				if (!computationParams.modelInfo.IsEmpty())
					resourcesJson[MODEL_INFO] = computationParams.modelInfo.ToJson(false);

				if (!computationParams.avatarModifications.IsEmpty())
					resourcesJson[AVATAR_MODIFICATIONS] = computationParams.avatarModifications.ToJson(false);

				if (!computationParams.shapeModifications.IsEmpty())
					resourcesJson[SHAPE_MODIFICATIONS] = computationParams.shapeModifications.ToJson();
			}

			return resourcesJson.ToString(4);
		}

		/// <summary>
		/// Returns lists of parameters for a given subset that can be generated for avatar
		/// </summary>
		public ComputationParameters GetParameters(ComputationParametersSubset parametersSubset, PipelineType pipelineType)
		{
			if (computationParametersDictionary.ContainsKey(pipelineType.ParametersSubsetHash(parametersSubset)))
			{
				return computationParametersDictionary[pipelineType.ParametersSubsetHash(parametersSubset)];
			}
			else
			{
				ComputationParameters computationParameters = ComputationParameters.Empty;

				if (pipelineType == PipelineType.UMA_MALE || pipelineType == PipelineType.UMA_FEMALE)
				{
					if (parametersSubset == ComputationParametersSubset.ALL)
						computationParameters = GetUmaParameters();
				}
				else
				{
					AvatarSdkPipelineSubtype sdkSubtype = pipelineType.ToSdkPipelineSubType();
					if (parametersSubset == ComputationParametersSubset.DEFAULT)
						computationParameters = GetParametersForLocalComputeSubset(LocalComputeParametersSubset.Default, sdkSubtype);
					else
					{
						// Merge Base parameters with Custom to get the list of all available parameters
						computationParameters = GetParametersForLocalComputeSubset(LocalComputeParametersSubset.Base, sdkSubtype);
						ComputationParameters customParameters = GetParametersForLocalComputeSubset(LocalComputeParametersSubset.Custom, sdkSubtype);
						computationParameters.Merge(customParameters);
					}
				}

				computationParametersDictionary.Add(pipelineType.ParametersSubsetHash(parametersSubset), computationParameters);
				return computationParameters;
			}
		}

		private unsafe ComputationParameters GetParametersForLocalComputeSubset(LocalComputeParametersSubset localComputeSubset, AvatarSdkPipelineSubtype pipelineSubtype)
		{
			byte[] buffer = new byte[32768];
			int jsonLength = 0;
			fixed (byte* rawBytes = &buffer[0])
			{
				switch (localComputeSubset)
				{
					case LocalComputeParametersSubset.Base:
						jsonLength = getParametersJson(pipelineSubtype, rawBytes, buffer.Length);
						break;

					case LocalComputeParametersSubset.Default:
						jsonLength = getDefaultParametersJson(pipelineSubtype, rawBytes, buffer.Length);
						break;

					case LocalComputeParametersSubset.Custom:
						jsonLength = getCustomParametersJson(pipelineSubtype, rawBytes, buffer.Length);
						break;
				}
			}

			if (jsonLength < 0)
			{
				if (localComputeSubset != LocalComputeParametersSubset.Custom)
					Debug.LogErrorFormat("Error during getting paranmeters json. Parameters subset: {0}, pipeline subtype: {1}", localComputeSubset, pipelineSubtype);
				return ComputationParameters.Empty;
			}

			return GetParametersFromJson(System.Text.Encoding.ASCII.GetString(buffer, 0, jsonLength));
		}

		private unsafe ComputationParameters GetUmaParameters()
		{
			byte[] buffer = new byte[32768];
			int jsonLength = 0;
			fixed (byte* rawBytes = &buffer[0])
				jsonLength = getUmaParametersJson(rawBytes, buffer.Length);

			if (jsonLength < 0)
			{
				Debug.LogError("Error during getting uma paranmeters json");
				return ComputationParameters.Empty;
			}

			return GetParametersFromJson(System.Text.Encoding.ASCII.GetString(buffer, 0, jsonLength));
		}
	}
}
