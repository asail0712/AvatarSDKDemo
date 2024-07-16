/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ItSeez3D.AvatarSdk.Core
{
	/// <summary>
	/// Base class that contains common methods for LocalComputeParametersController and CloudParametersController
	/// </summary>
	public abstract class ComputationParametersController
	{
		protected const string PIPELINE_KEY = "pipeline";
		protected const string PIPELINE_SUBTYPE_KEY = "pipeline_subtype";
		protected const string BLENDSHAPES_KEY = "blendshapes";
		protected const string HAIRCUTS_KEY = "haircuts";
		protected const string AVATAR_MODIFICATIONS = "avatar_modifications";
		protected const string MODEL_INFO = "model_info";
		protected const string SHAPE_MODIFICATIONS = "shape_modifications";
		protected const string ADDITIONAL_TEXTURES = "additional_textures";

		/// <summary>
		/// Converts AvatarParameters to the JSON format required for the avatar calculating
		/// </summary>
		public abstract string GetCalculationParametersJson(PipelineType pipelineType, ComputationParameters computationParams);

		/// <summary>
		/// Parses JSON to AvatarParameters
		/// </summary>
		public static ComputationParameters GetParametersFromJson(string json)
		{
			ComputationParameters computationParams = ComputationParameters.Empty;
			var rootNode = JSON.Parse(json);
			if (rootNode != null)
			{
				var blendshapesRootNode = FindNodeByName(rootNode, BLENDSHAPES_KEY);
				if (blendshapesRootNode != null)
					computationParams.blendshapes = new ComputationList(blendshapesRootNode);

				var haircutsRootNode = FindNodeByName(rootNode, HAIRCUTS_KEY);
				if (haircutsRootNode != null)
					computationParams.haircuts = new ComputationList(haircutsRootNode);

				computationParams.avatarModifications.SetPropertiesToUnavailableState();
				var avatarModificationsNode = FindNodeByName(rootNode, AVATAR_MODIFICATIONS);
				if (avatarModificationsNode != null)
					computationParams.avatarModifications.FromJson(avatarModificationsNode);

				computationParams.modelInfo.SetPropertiesToUnavailableState();
				var modelInfoNode = FindNodeByName(rootNode, MODEL_INFO);
				if (modelInfoNode != null)
					computationParams.modelInfo.FromJson(modelInfoNode);

				computationParams.shapeModifications.SetPropertiesToUnavailableState();
				var shapeModificationsNode = FindNodeByName(rootNode, SHAPE_MODIFICATIONS);
				if (shapeModificationsNode != null)
					computationParams.shapeModifications.FromJson(shapeModificationsNode);

				var additionalTexturesNode = FindNodeByName(rootNode, ADDITIONAL_TEXTURES);
				if (additionalTexturesNode != null)
					computationParams.additionalTextures = new ComputationList(additionalTexturesNode);
			}
			return computationParams;
		}

		/// <summary>
		/// Recursive finds Node with the given name in JSON
		/// </summary>
		protected static JSONNode FindNodeByName(JSONNode rootNode, string name)
		{
			if (rootNode == null)
				return null;

			var node = rootNode[name];
			if (node != null)
				return node;

			foreach (JSONNode childNode in rootNode.Children)
			{
				node = FindNodeByName(childNode, name);
				if (node != null)
					return node;
			}

			return null;
		}

		/// <summary>
		/// Checks if list is null or empty
		/// </summary>
		protected static bool IsListNullOrEmpty<T>(List<T> list)
		{
			return list == null || list.Count == 0;
		}
	}
}
