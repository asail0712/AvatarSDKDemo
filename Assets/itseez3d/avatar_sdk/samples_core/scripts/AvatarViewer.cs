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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ItSeez3D.AvatarSdk.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ItSeez3D.AvatarSdkSamples.SamplePipelineTraits;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ItSeez3D.AvatarSdkSamples.Core
{
	public class AvatarViewer : MonoBehaviour
	{
		public class SceneParams
		{
			public string avatarCode;
			public string sceneToReturn;
			public IAvatarProvider avatarProvider;
			public bool showSettings = true;
			public bool useAnimations = true;
		};

		#region UI

		public GameObject avatarControls;
		public Text progressText;
		public Image photoPreview;
		public Button convertToObjButton;
		public Button fbxExportButton;
		public Button prefabButton;
		public Button downloadButton;
		public GameObject settingsPanel;
		public GameObject pipelinesPanel;
		public Text haircutText;
		public Text blendshapeText;
		public GameObject haircutsPanel;
		public GameObject animationsPanel;
		public GameObject blendshapesPanel;
		public ItemsSelectingView blendshapesSelectingView;
		public HaircutsSelectingView haircutsSelectingView;
		public Button modelInfoButton;
		public ModelInfoDataPanel modelInfoPanel;
		public ColorPicker haircutColorPicker;

		public AnimationManager animationManager;

		public RuntimeAnimatorController legacyAnimationsController;
		public RuntimeAnimatorController mobileAnimationsController;

		#endregion

		#region private memebers

		// Parameters needed to initialize scene and show avatar. Should be set before showing the viewer scene
		protected static SceneParams initParams = null;

		// Current displayed avatar
		protected string currentAvatarCode;

		// Scene that will be shown after clicking on the back button
		private string sceneToReturn;

		// This GameObject represents head in the scene.
		protected GameObject headObject = null;

		// This GameObject represents haircut in the scene
		protected GameObject haircutObject = null;

		// Array of haircut names
		private string[] avatarHaircuts = null;

		// Haircut index of the current avatar, zero for bald head.
		private int currentHaircut = 0;

		// AvatarProvider to retrieve head mesh and texture
		protected IAvatarProvider avatarProvider;

		// True is animations will be used, in other case single blendshapes will be used
		private bool useAnimations = true;

		// Blendshapes names with their index in avatar mesh
		private Dictionary<int, string> availableBlendshapes = new Dictionary<int, string>();

		// Blendshape index of the current avatar
		private int currentBlendshape = 0;

		// Cached haircuts for avatars
		private Dictionary<string, string[]> cachedHaircuts = new Dictionary<string, string[]>();

		private AvatarShaderType shaderType = AvatarShaderType.AvatarSdkUnlit;

		protected ModelExporter modelExporter = null;

		#endregion

		#region Constants

		private const string BALD_HAIRCUT_NAME = "bald";
		private const string GENERATED_HAIRCUT_NAME = "generated";
		private const string HEAD_OBJECT_NAME = "ItSeez3D Head";
		private const string HAIRCUT_OBJECT_NAME = "ItSeez3D Haircut";
		private const string AVATAR_OBJECT_NAME = "ItSeez3D Avatar";

		#endregion

		#region Methods to call event handlers

		private void OnDisplayedHaircutChanged(string newHaircutId)
		{
			int slashPos = newHaircutId.LastIndexOfAny(new char[] { '\\', '/' });
			haircutText.text = slashPos == -1 ? newHaircutId : newHaircutId.Substring(slashPos + 1);

			haircutColorPicker.gameObject.SetActive(newHaircutId != BALD_HAIRCUT_NAME);
		}

		#endregion

		#region static methods

		public static void SetSceneParams(SceneParams sceneParams)
		{
			initParams = sceneParams;
		}

		#endregion

		#region Lifecycle

		protected void Start()
		{
			avatarControls.SetActive(false);

			// required for transparent hair shader
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_EDITOR
			QualitySettings.antiAliasing = 8;
#elif !UNITY_WEBGL
			QualitySettings.antiAliasing = 4;
#endif

			StartCoroutine(InitializeScene());
		}

		#endregion

		#region UI controls events handling

		/// <summary>
		/// Button click handler. Go back to the gallery.
		/// </summary>
		public virtual void OnBack()
		{
			SceneManager.LoadScene(sceneToReturn);
		}

		public void OnPrevHaircut()
		{
			StartCoroutine(ChangeHaircut(currentHaircut - 1));
		}

		public void OnNextHaircut()
		{
			StartCoroutine(ChangeHaircut(currentHaircut + 1));
		}

		public void OnPrevBlendshape()
		{
			ChangeCurrentBlendshape(currentBlendshape - 1);
		}

		public void OnNextBlendshape()
		{
			ChangeCurrentBlendshape(currentBlendshape + 1);
		}

		public void OnBlendshapeListButtonClick()
		{
			avatarControls.SetActive(false);
			blendshapesSelectingView.Show(new List<string>() { availableBlendshapes[currentBlendshape] }, list =>
			{
				avatarControls.SetActive(true);
				// Find KeyValuePair for selected blendshape name. Assume that returned list contains only one element.
				var pair = availableBlendshapes.FirstOrDefault(p => p.Value == list[0]);
				ChangeCurrentBlendshape(pair.Key);
			});
		}

		public void OnHaircutListButtonClick()
		{
			avatarControls.SetActive(false);
			haircutsSelectingView.Show(new List<string>() { avatarHaircuts[currentHaircut] }, list =>
			{
				avatarControls.SetActive(true);
				// Find index of the selected haircut.
				for (int i = 0; i < avatarHaircuts.Length; i++)
				{
					if (avatarHaircuts[i] == list[0])
					{
						StartCoroutine(ChangeHaircut(i));
						break;
					}
				}
			});
		}

		public void OnUnlitShaderToggleChanged(bool isChecked)
		{
			if (isChecked)
			{
				shaderType = AvatarShaderType.AvatarSdkUnlit;
				UpdateAvatarMaterials();
			}
		}

		public void OnLitShaderToggleChanged(bool isChecked)
		{
			if (isChecked)
			{
				shaderType = AvatarShaderType.AvatarSdkLit;
				UpdateAvatarMaterials();
			}
		}

		public void OnStandardShaderToggleChanged(bool isChecked)
		{
			if (isChecked)
			{
				shaderType = AvatarShaderType.Standard;
				UpdateAvatarMaterials();
			}
		}


		public void ConvertAvatarToObjFormat()
		{
			var outputDir = AvatarSdkMgr.Storage().GetAvatarSubdirectory(currentAvatarCode, AvatarSubdirectory.OBJ_EXPORT);
			StartCoroutine(ExportAvatarAsync(outputDir, MeshFileFormat.OBJ));
		}

		public void ExportAvatarAsFbx()
		{
			var outputDir = AvatarSdkMgr.Storage().GetAvatarSubdirectory(currentAvatarCode, AvatarSubdirectory.FBX_EXPORT);
			StartCoroutine(ExportAvatarAsync(outputDir, MeshFileFormat.FBX));
		}

		private IEnumerator ExportAvatarAsync(string outputDir, MeshFileFormat meshFormat)
		{
			ChangeControlsInteractability(false);
			progressText.text = "Exporting model...";

			modelExporter.meshFormat = meshFormat;
			yield return modelExporter.ExportModelAsync(outputDir);

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
			progressText.text = string.Format("{0} model was saved in avatar's directory.", meshFormat);
			Debug.LogFormat("{0} model was saved in {1}", meshFormat, outputDir);


			ChangeControlsInteractability(true);
		}

		public void CreateAvatarPrefab()
		{
#if UNITY_EDITOR
			string prefabDir = Path.Combine(PluginStructure.GetPluginDirectoryPath(PluginStructure.PREFABS_DIR, PathOriginOptions.RelativeToAssetsFolder), currentAvatarCode);
			AvatarPrefabBuilder.CreateAvatarPrefab(prefabDir, GameObject.Find(AVATAR_OBJECT_NAME));
#endif
		}

		#endregion

		#region Async utils

		/// <summary>
		/// Helper function that waits until async request finishes and keeps track of progress on request and it's
		/// subrequests. Note it does "yield return null" every time, which means that code inside the loop
		/// is executed on each frame, but after progress is updated the function does not block the main thread anymore.
		/// </summary>
		/// <param name="r">Async request to await.</param>
		protected IEnumerator Await(AsyncRequest r, bool showPercents = true)
		{
			while (!r.IsDone)
			{
				yield return null;

				if (r.IsError)
				{
					Debug.LogError(r.ErrorMessage);
					yield break;
				}

				if (showPercents)
					progressText.text = string.Format("{0}: {1}%", r.State, r.ProgressPercent.ToString("0.0"));
				else
					progressText.text = string.Format("{0} ...", r.State);
			}

			progressText.text = string.Empty;
		}

		#endregion

		#region Initialization routine

		private IEnumerator InitializeScene()
		{
			if (initParams != null)
			{
				avatarProvider = initParams.avatarProvider;
				sceneToReturn = initParams.sceneToReturn;
				currentAvatarCode = initParams.avatarCode;
				useAnimations = initParams.useAnimations;

				settingsPanel.SetActive(initParams.showSettings);
				animationsPanel.SetActive(initParams.useAnimations);
				blendshapesPanel.SetActive(!initParams.useAnimations);

				InitilizeUIControls();

				initParams = null;

				yield return ShowAvatar(currentAvatarCode);
			}
			else
				Debug.LogError("Scene parameters were no set!");
		}

		protected virtual void InitilizeUIControls()
		{
			if (MeshConverter.IsExportAvailable)
				convertToObjButton.gameObject.SetActive(true);

			if (MeshConverter.IsFbxFormatSupported)
				fbxExportButton.gameObject.SetActive(true);

#if UNITY_EDITOR_WIN
			prefabButton.gameObject.SetActive(true);
#endif
		}

		#endregion

		#region Avatar processing

		/// <summary>
		/// Show avatar in the scene. Also load haircut information to allow further haircut change.
		/// </summary>
		protected IEnumerator ShowAvatar(string avatarCode)
		{
			ChangeControlsInteractability(false);
			yield return new WaitForSeconds(0.05f);

			StartCoroutine(SampleUtils.DisplayPhotoPreview(avatarCode, photoPreview));

			progressText.text = string.Empty;
			currentHaircut = 0;

			var currentAvatar = GameObject.Find(AVATAR_OBJECT_NAME);
			if (currentAvatar != null)
				Destroy(currentAvatar);

			var avatarObject = new GameObject(AVATAR_OBJECT_NAME);
			var headMeshRequest = avatarProvider.GetHeadMeshAsync(avatarCode, true);
			yield return Await(headMeshRequest);

			if (headMeshRequest.IsError)
			{
				Debug.LogError("Could not load avatar from disk!");
			}
			else
			{
				PipelineType pipelineType = CoreTools.LoadPipelineType(avatarCode);
				TexturedMesh texturedMesh = headMeshRequest.Result;

				// game object can be deleted if we opened another avatar
				if (avatarObject != null && avatarObject.activeSelf)
				{
					avatarObject.AddComponent<RotateByMouse>();

					headObject = new GameObject(HEAD_OBJECT_NAME);
					headObject.SetActive(false);
					var meshRenderer = headObject.AddComponent<SkinnedMeshRenderer>();
					meshRenderer.sharedMesh = texturedMesh.mesh;
					meshRenderer.material = MaterialAdjuster.GetHeadMaterial(avatarCode, texturedMesh.texture, shaderType);
					headObject.transform.SetParent(avatarObject.transform);
					SetAvatarScale(avatarCode, avatarObject.transform, pipelineType);

					if (useAnimations)
					{
						animationManager.animatorController = GetAnimationsController(pipelineType);
						animationManager.CreateAnimator(headObject);
					}
					else
					{
						//add an empty blendshape with index -1
						availableBlendshapes.Add(-1, "None");

						var mesh = headObject.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
						for (int i = 0; i < mesh.blendShapeCount; i++)
							availableBlendshapes.Add(i, mesh.GetBlendShapeName(i));
						ChangeCurrentBlendshape(-1);
						blendshapesSelectingView.InitItems(availableBlendshapes.Values.ToList());

						if (availableBlendshapes.Count == 1)
							blendshapesPanel.SetActive(false);
					}

					modelExporter = avatarObject.AddComponent<ModelExporter>();
				}
			}

			var haircutsIdsRequest = GetHaircutsIdsAsync(avatarCode);
			yield return Await(haircutsIdsRequest);
			string[] haircuts = haircutsIdsRequest.Result;
			if (haircuts != null && haircuts.Length > 0)
			{
				//Add fake "bald" haircut
				var haircutsList = haircuts.ToList();
				MoveGeneratedHaircutInStartPosition(haircutsList);
				haircutsList.Insert(0, BALD_HAIRCUT_NAME);
				avatarHaircuts = haircutsList.ToArray();
				var defaultHaircut = PipelineSampleTraitsFactory.Instance.GetTraitsFromAvatarCode(avatarCode).GetDefaultAvatarHaircut(avatarCode);
				currentHaircut = findCurrentHaircut(haircutsList, defaultHaircut);
				haircutsSelectingView.InitItems(avatarCode, avatarHaircuts.ToList(), avatarProvider);
				haircutsPanel.SetActive(true);
			}
			else
			{
				avatarHaircuts = new string[1];
				avatarHaircuts[0] = BALD_HAIRCUT_NAME;
				haircutsPanel.SetActive(false);
				OnDisplayedHaircutChanged(BALD_HAIRCUT_NAME);
			}


			if(haircuts != null)
			{
				yield return ChangeHaircutFunc(currentHaircut, false);
				OnDisplayedHaircutChanged(avatarHaircuts[currentHaircut]);
			}
			if (haircutObject != null)
			{
				haircutObject.SetActive(true);
			}
			ChangeControlsInteractability(true);
			headObject.SetActive(true);
			avatarControls.SetActive(true);

			ModelInfo modelInfo = CoreTools.GetAvatarModelInfo(currentAvatarCode);
			modelInfoPanel.UpdateData(modelInfo);
		}

		private void UpdateAvatarMaterials()
		{
			var headMeshRenderer = headObject.GetComponent<SkinnedMeshRenderer>();
			headMeshRenderer.material = MaterialAdjuster.GetHeadMaterial(currentAvatarCode, headMeshRenderer.material.mainTexture, shaderType);

			if (haircutObject != null)
			{
				MeshRenderer haircutMeshRenderer = haircutObject.GetComponent<MeshRenderer>();
				haircutMeshRenderer.material = MaterialAdjuster.GetHaircutMaterial(haircutMeshRenderer.material.mainTexture, GetCurrentHaircutName(), shaderType);

				var haircutRecolorer = haircutObject.GetComponent<HaircutRecolorer>();
				if (haircutRecolorer != null)
					haircutRecolorer.UpdateMaterialToRecolor(haircutMeshRenderer.material);
			}

			Resources.UnloadUnusedAssets();
		}

		private void AddHaircutRecolorerComponent(string avatarCode, GameObject avatarHaircut)
		{
			HaircutRecolorer haircutRecolorer = avatarHaircut.AddComponent<HaircutRecolorer>();
			haircutRecolorer.colorPicker = haircutColorPicker;
			haircutRecolorer.DetectDefaultColor(avatarCode);
		}

		private int findCurrentHaircut(List<string> haircutsList, string v)
		{
			int result = haircutsList.FindIndex(hc => hc.Contains(v));
			return result == -1 ? 0 : result;
		}

		/// <summary>
		/// Requests haircuts identities from the server or takes them from the cache
		/// </summary>
		protected AsyncRequest<string[]> GetHaircutsIdsAsync(string avatarCode)
		{
			var request = new AsyncRequest<string[]>(Strings.GettingAvailableHaircuts);
			StartCoroutine(GetHaircutsIdsFunc(avatarCode, request));
			return request;
		}

		private IEnumerator GetHaircutsIdsFunc(string avatarCode, AsyncRequest<string[]> request)
		{
			string[] haircuts = null;
			if (cachedHaircuts.ContainsKey(avatarCode))
				haircuts = cachedHaircuts[avatarCode];
			else
			{
				var haircutsRequest = avatarProvider.GetHaircutsIdAsync(avatarCode);
				yield return request.AwaitSubrequest(haircutsRequest, 1.0f);
				if (request.IsError)
					yield break;

				haircuts = ReorderHaircutIds(haircutsRequest.Result);
				cachedHaircuts[avatarCode] = haircuts;
			}
			request.IsDone = true;
			request.Result = haircuts;
		}

		private string[] ReorderHaircutIds(string[] haircuts)
		{
			if (haircuts == null)
				return null;

			List<string> baseHaircuts = new List<string>();
			List<string> facegenHaircuts = new List<string>();
			foreach(string h in haircuts)
			{
				if (h.Contains("facegen"))
					facegenHaircuts.Add(h);
				else
					baseHaircuts.Add(h);
			}
			baseHaircuts.AddRange(facegenHaircuts);
			return baseHaircuts.ToArray();
		}

		private void SetAvatarScale(string avatarCode, Transform avatarTransform, PipelineType pipelineType)
		{
			var sampleTraits = pipelineType.SampleTraits();
			avatarTransform.position = sampleTraits.ViewerLocalPosition;
			avatarTransform.localScale = sampleTraits.ViewerDisplayScale;
		}

		#endregion

		#region Haircut handling

		/// <summary>
		/// Change the displayed haircut. Make controls inactive while haircut is being loaded to prevent
		/// multiple coroutines running at once.
		/// </summary>
		/// <param name="newIdx">New haircut index.</param>
		private IEnumerator ChangeHaircut(int newIdx)
		{
			ChangeControlsInteractability(false);

			var previousIdx = currentHaircut;
			yield return StartCoroutine(ChangeHaircutFunc(newIdx));
			if (previousIdx != currentHaircut)
				OnDisplayedHaircutChanged(avatarHaircuts[currentHaircut]);

			ChangeControlsInteractability(true);
		}

		/// <summary>
		/// Actually load the haircut model and texture and display it in the scene (aligned with the head).
		/// </summary>
		/// <param name="newIdx">Index of the haircut.</param>
		private IEnumerator ChangeHaircutFunc(int newIdx, bool displayImmediate = true)
		{
			if (newIdx < 0)
				newIdx = avatarHaircuts.Length - 1;
			if (newIdx >= avatarHaircuts.Length)
				newIdx = 0;

			currentHaircut = newIdx;
			string haircutName = avatarHaircuts[currentHaircut];

			// bald head is just absence of haircut
			if (string.Compare(haircutName, BALD_HAIRCUT_NAME) == 0)
			{
				Destroy(haircutObject);
				haircutObject = null;
				yield break;
			}

			var haircurtMeshRequest = avatarProvider.GetHaircutMeshAsync(currentAvatarCode, haircutName);
			yield return Await(haircurtMeshRequest);
			if (haircurtMeshRequest.IsError)
				yield break;

			Destroy(haircutObject);

			var texturedMesh = haircurtMeshRequest.Result;
			haircutObject = new GameObject(HAIRCUT_OBJECT_NAME);
			if(!displayImmediate)
			{
				haircutObject.SetActive(false);
			}
			
			haircutObject.AddComponent<MeshFilter>().mesh = texturedMesh.mesh;
			var meshRenderer = haircutObject.AddComponent<MeshRenderer>();

			meshRenderer.material = MaterialAdjuster.GetHaircutMaterial(texturedMesh.texture, haircutName, shaderType);

			// ensure that haircut is rotated just like the head
			var avatarObject = GameObject.Find(AVATAR_OBJECT_NAME);
			if (avatarObject != null)
			{
				haircutObject.transform.SetParent(avatarObject.transform);
				haircutObject.transform.localRotation = Quaternion.identity;
				haircutObject.transform.localPosition = Vector3.zero;
				haircutObject.transform.localScale = Vector3.one;
			}

			AddHaircutRecolorerComponent(currentAvatarCode, haircutObject);

			yield return null;  // only after the next frame the textures and materials are actually updated in the scene
		}

		protected string GetCurrentHaircutName()
		{
			if (avatarHaircuts != null && string.Compare(avatarHaircuts[currentHaircut], BALD_HAIRCUT_NAME) != 0)
				return avatarHaircuts[currentHaircut];
			else
				return string.Empty;
		}

		private void MoveGeneratedHaircutInStartPosition(List<string> haircuts)
		{
			string generatedHaircutFullName = haircuts.FirstOrDefault(h => h.Contains(GENERATED_HAIRCUT_NAME));
			if (!string.IsNullOrEmpty(generatedHaircutFullName))
			{
				haircuts.Remove(generatedHaircutFullName);
				haircuts.Insert(0, generatedHaircutFullName);
			}
		}

		#endregion

		#region Blendshapes handling
		private void ChangeCurrentBlendshape(int newIdx)
		{
			if (!availableBlendshapes.ContainsKey(newIdx))
				return;

			currentBlendshape = newIdx;

			var meshRenderer = headObject.GetComponentInChildren<SkinnedMeshRenderer>();
			foreach (int idx in availableBlendshapes.Keys)
			{
				if (idx >= 0)
					meshRenderer.SetBlendShapeWeight(idx, idx == currentBlendshape ? 100.0f : 0.0f);
			}

			blendshapeText.text = availableBlendshapes[currentBlendshape];
		}

		private RuntimeAnimatorController GetAnimationsController(PipelineType pipelineType)
		{
			if (pipelineType == PipelineType.HEAD_2_0_BUST_MOBILE || pipelineType == PipelineType.HEAD_2_0_HEAD_MOBILE || pipelineType == PipelineType.HEAD_1_2)
				return mobileAnimationsController;
			
			return legacyAnimationsController;
		}
		#endregion

		#region UI controls handling
		protected virtual void ChangeControlsInteractability(bool isEnabled)
		{
			foreach (var control in avatarControls.GetComponentsInChildren<Selectable>())
				control.interactable = isEnabled;
		}
		#endregion
	}
}
