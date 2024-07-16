using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

using ItSeez3D.AvatarSdk.Core;

using XPlan;
using XPlan.Extensions;
using XPlan.Interface;
using XPlan.Net;
using XPlan.Observe;
using XPlan.Utility;

namespace Granden.AvatarSDK
{
    public class GenerateHeadMsg : MessageBase
    {
        public Texture2D photoTexture;
        public Action<GameObject> finsihAction;

        public GenerateHeadMsg(Texture2D photoTexture, Action<GameObject> finsihAction)
        {
            this.photoTexture = photoTexture;
            this.finsihAction = finsihAction;
        }
    }

    public class AvatarParam
	{
        public bool bAddHair        = true;
        public bool bAddMotion      = true;
        public bool bHairRecolor    = true;
        public bool bSkinRecolor    = true;
    }

    public class AvatarSDKHandler : LogicComponentBase
    {
        private const string HEAD_OBJECT_NAME       = "ItSeez3D Head";
        private const string HAIRCUT_OBJECT_NAME    = "ItSeez3D Haircut";
        private const string BaseHaircutName        = "base/generated";
        private const string animationPath          = "animator/facial_animation_controller";

#if AVATARSDK_CLOUD
        private SdkType sdkType                     = SdkType.Cloud;
#elif AVATARSDK_LOCAL
        private SdkType sdkType                     = SdkType.LocalCompute;
#else
        private SdkType sdkType                     = SdkType.Cloud;
#endif
        private PipelineType headPipeline           = PipelineType.HEAD_2_0_HEAD_MOBILE;

        private bool bIsInitial                                             = false;
        private IAvatarProvider avatarProvider                              = null;

        public AvatarSDKHandler(ILicenseGetter licenseGetter, AvatarParam avatarParam, ICommand parametersCommand, IValueReference<ModificationParam> parametersValue)
        {
#if AVATARSDK_CLOUD
            licenseGetter.InitialCredential((bSuccess) =>
            {
                InitialAvatarSDK((bSuccess) => 
                { 
                    if(!bSuccess)
					{
                        SendMsg<ErrorNotifyMsg>(ErrorDefine.AVATARSDK_INITIAL_FAILED);
                        return;
					}

                    bIsInitial = bSuccess;

                    Debug.Log("Avatar SDK 初始化成功");
                });
            });
#elif AVATARSDK_LOCAL
            InitialAvatarSDK((bSuccess) =>
            {
                if (!bSuccess)
                {
                    SendMsg<ErrorNotifyMsg>(ErrorDefine.AVATARSDK_INITIAL_FAILED, ErrorLevel.Error);
                    return;
                }

                bIsInitial = bSuccess;

                Debug.Log("Avatar SDK 初始化成功");
            });
#endif
            RegisterNotify<GenerateHeadMsg>(this, (msg)=> 
            {
                if (!bIsInitial)
                {
                    SendMsg<ErrorNotifyMsg>(ErrorDefine.AVATARSDK_INITIAL_FAILED, ErrorLevel.Error);
                    return;
                }

                byte[] beautyByte   = msg.photoTexture.ToByteArray();

                StartCoroutine(GenerateAndDisplayHead(avatarParam, parametersCommand, parametersValue, beautyByte, false, (resultGO) =>
                {
                    msg.finsihAction?.Invoke(resultGO);
                }));
            });
        }

		protected override void OnDispose(bool bAppQuit)
		{
			if(bAppQuit)
			{
                return;
			}
        }

        /*******************************
        * Avatar SDK Initial
        * *****************************/
        private void InitialAvatarSDK(Action<bool> onFinish)
		{
            if (!AvatarSdkMgr.IsInitialized)
            {
                AvatarSdkMgr.Init(sdkType: sdkType);
            }

            StartCoroutine(InitialAvatarProvider(onFinish));
        }

        private IEnumerator InitialAvatarProvider(Action<bool> onFinish)
		{
            avatarProvider = AvatarSdkMgr.GetAvatarProvider();

            if (!avatarProvider.IsInitialized)
            {
                AsyncRequest initializeRequest = avatarProvider.InitializeAsync();
                yield return Await(initializeRequest);
                if (initializeRequest.IsError)
                {
                    onFinish?.Invoke(false);
                    yield break;
                }
            }

            onFinish?.Invoke(true);
        }

        /*******************************
        * Generate Head
        * *****************************/
        public IEnumerator GenerateAndDisplayHead(AvatarParam avatarParam, ICommand parametersCommand, IValueReference<ModificationParam> parametersValue, byte[] photoBytes, bool bWithBlendShape, Action<GameObject> onFinish)
        {
            ComputationParameters computationParameters = ComputationParameters.Empty;
            yield return ConfigureComputationParameters(parametersCommand, parametersValue, headPipeline, computationParameters);

            // generate avatar from the photo and get its code in the Result of request
            var initializeRequest = avatarProvider.InitializeAvatarAsync(photoBytes, "name", "description", headPipeline, computationParameters);
            yield return Await(initializeRequest, (progress) => 
            {
                float currProgress = 0.3f + 0.2f * progress * 0.01f;

                //Debug.Log("目前進度 " + currProgress);

                SendMsg<ProgressNotifyMsg>(currProgress);
            });
            if (initializeRequest.IsError)
            {
                SendMsg<ErrorNotifyMsg>(ErrorDefine.GENERATED_HEAD_FAILED, ErrorLevel.Error);
                onFinish?.Invoke(null);
                yield break;
            }

            SendMsg<ProgressNotifyMsg>(0.5f);

            string currentAvatarCode    = initializeRequest.Result;            
            var calculateRequest        = avatarProvider.StartAndAwaitAvatarCalculationAsync(currentAvatarCode);
            yield return Await(calculateRequest, (progress) =>
            {
                float currProgress = 0.5f + 0.2f * progress * 0.01f;

                //Debug.Log("目前進度 " + currProgress);

                SendMsg<ProgressNotifyMsg>(currProgress);
            });
            if (calculateRequest.IsError)
            {
                SendMsg<ErrorNotifyMsg>(ErrorDefine.GENERATED_HEAD_FAILED, ErrorLevel.Error);
                onFinish?.Invoke(null);
                yield break;
            }

            SendMsg<ProgressNotifyMsg>(0.7f);

            // 取得頭部模型
            AsyncRequest<TexturedMesh> avatarHeadRequest = avatarProvider.GetHeadMeshAsync(currentAvatarCode, avatarParam.bAddMotion);
            yield return Await(avatarHeadRequest, (progress) =>
            {
                float currProgress = 0.7f + 0.2f * progress * 0.01f;

                //Debug.Log("目前進度 " + currProgress);

                SendMsg<ProgressNotifyMsg>(currProgress);
            });
            if (avatarHeadRequest.IsError)
            {
                SendMsg<ErrorNotifyMsg>(ErrorDefine.GENERATED_HEAD_FAILED, ErrorLevel.Error);
                onFinish?.Invoke(null);
                yield break;
            }

            SendMsg<ProgressNotifyMsg>(0.9f);

            // 取得頭髮模型
            AsyncRequest<TexturedMesh> avatarHaircurtRequest = null;

            if (avatarParam.bAddHair)
            {
                var haircutsIdsRequest = GetHaircutsIdsAsync(currentAvatarCode);
                yield return Await(haircutsIdsRequest, (progress) =>
                {
                    float currProgress = 0.9f + 0.1f * progress * 0.01f;

                    //Debug.Log("目前進度 " + currProgress);

                    SendMsg<ProgressNotifyMsg>(currProgress);
                });
                string[] haircuts = haircutsIdsRequest.Result;
                if (haircutsIdsRequest.IsError || (haircuts == null && haircuts.Length == 0))
                {
                    SendMsg<ErrorNotifyMsg>(ErrorDefine.GENERATED_HAIR_FAILED, ErrorLevel.Error);
                    onFinish?.Invoke(null);
                    yield break;
                }

                SendMsg<ProgressNotifyMsg>(1f);

                if (haircuts.Contains<string>(BaseHaircutName))
                {
                    avatarHaircurtRequest = avatarProvider.GetHaircutMeshAsync(currentAvatarCode, BaseHaircutName);
                    yield return Await(avatarHaircurtRequest, (progress) =>
                    {
                        float currProgress = 1f + 0.1f * progress * 0.01f;

                        //Debug.Log("目前進度 " + currProgress);

                        SendMsg<ProgressNotifyMsg>(currProgress);
                    });
                    if (avatarHaircurtRequest.IsError)
                    {
                        SendMsg<ErrorNotifyMsg>(ErrorDefine.GENERATED_HAIR_FAILED, ErrorLevel.Error);
                        onFinish?.Invoke(null);
                        yield break;
                    }
                }
            }

            SendMsg<ProgressNotifyMsg>(1.1f);

            // 模型 gameobject設定
            GameObject resultObj    = new GameObject("Avatar Result");
            GameObject headObj      = DisplayHead(currentAvatarCode, avatarHeadRequest.Result);
            GameObject hairObj      = null;

            headObj.SetActive(true);
            PostProcessHead(avatarParam, ref headObj);

            resultObj.AddChild(headObj);

            // 有頭髮才需要attach
            if (avatarHaircurtRequest != null)
            {
                hairObj = DisplayHair(currentAvatarCode, avatarHaircurtRequest.Result);
                hairObj.SetActive(true);
                PostProcessHair(avatarParam, ref hairObj);

                resultObj.AddChild(hairObj);
            }

            onFinish?.Invoke(resultObj);
            Debug.Log($"Generate avatar head Ok!");
        }

        protected virtual IEnumerator ConfigureComputationParameters(ICommand parametersCommand, IValueReference<ModificationParam> parametersValue, PipelineType pipelineType, ComputationParameters computationParameters)
        {
            // Choose default set of parameters
            var parametersRequest = avatarProvider.GetParametersAsync(ComputationParametersSubset.ALL, pipelineType);
            yield return Await(parametersRequest);

            if(parametersRequest.IsError)
			{
                SendMsg<ErrorNotifyMsg>(ErrorDefine.GET_PARAMETERS_FAILED, ErrorLevel.Error);

                yield break;
			}

            // 依照需求生成 parameters
            parametersCommand.Execute();

            computationParameters.haircuts              = parametersRequest.Result.haircuts;
            computationParameters.blendshapes           = parametersRequest.Result.blendshapes;
            computationParameters.avatarModifications   = parametersValue.Value.avatarModificationsGroup;
            computationParameters.shapeModifications    = parametersValue.Value.shapeModificationsGroup;
        }

        private GameObject DisplayHead(string avatarCode, TexturedMesh headMesh)
        {
            // create head object in the scene
            Debug.LogFormat("Generating Unity mesh object for head...");
            GameObject headObject = new GameObject(HEAD_OBJECT_NAME);
            headObject.SetActive(false);

            var headMeshRenderer        = headObject.AddComponent<SkinnedMeshRenderer>();
            headMeshRenderer.sharedMesh = headMesh.mesh;
#if AVATARSDK_CLOUD
            headMeshRenderer.material   = MaterialAdjuster.GetHeadMaterial(avatarCode, headMesh.texture, AvatarShaderType.UnlitShader);
#elif AVATARSDK_LOCAL
            headMeshRenderer.material   = MaterialAdjuster.GetHeadMaterial(avatarCode, headMesh.texture, AvatarShaderType.AvatarSdkUnlit);
#endif

            return headObject;
        }

        private GameObject DisplayHair(string avatarCode, TexturedMesh hairMesh)
        {
            GameObject haircutObject = new GameObject(HAIRCUT_OBJECT_NAME);            
            haircutObject.SetActive(false);
            
            haircutObject.AddComponent<MeshFilter>().mesh = hairMesh.mesh;
            var meshRenderer        = haircutObject.AddComponent<MeshRenderer>();
#if AVATARSDK_CLOUD
            meshRenderer.material   = MaterialAdjuster.GetHaircutMaterial(hairMesh.texture, BaseHaircutName, AvatarShaderType.UnlitShader);
#elif AVATARSDK_LOCAL
            meshRenderer.material   = MaterialAdjuster.GetHaircutMaterial(hairMesh.texture, BaseHaircutName, AvatarShaderType.AvatarSdkUnlit);
#endif

            return haircutObject;
        }

        protected AsyncRequest<string[]> GetHaircutsIdsAsync(string avatarCode)
        {
            var request = new AsyncRequest<string[]>(Strings.GettingAvailableHaircuts);
            StartCoroutine(GetHaircutsIdsFunc(avatarCode, request));
            return request;
        }

        private IEnumerator GetHaircutsIdsFunc(string avatarCode, AsyncRequest<string[]> request)
        {      
            var haircutsRequest = avatarProvider.GetHaircutsIdAsync(avatarCode);
            yield return request.AwaitSubrequest(haircutsRequest, 1.0f);

            if (request.IsError)
            { 
                yield break;
            }

            string[] haircuts   = haircutsRequest.Result;            
            request.IsDone      = true;
            request.Result      = haircuts;
        }

        /*******************************
        * Post Process
        * *****************************/
        private void PostProcessHead(AvatarParam avatarParam, ref GameObject headObj)
		{
            // 添加表情功能
            if(avatarParam.bAddMotion)
			{
                Animator motionAnimator                     = headObj.AddComponent<Animator>();
                motionAnimator.runtimeAnimatorController    = Resources.Load<RuntimeAnimatorController>(animationPath);
                motionAnimator.avatar                       = null;
                motionAnimator.applyRootMotion              = true;
                motionAnimator.cullingMode                  = AnimatorCullingMode.AlwaysAnimate;
                motionAnimator.updateMode                   = AnimatorUpdateMode.Normal;
            }

            if(avatarParam.bSkinRecolor)
			{

			}
		}

        private void PostProcessHair(AvatarParam avatarParam, ref GameObject hairObj)
        {
            if (avatarParam.bHairRecolor)
            {

            }
        }

        /*******************************
        * Avatar SDK other
        * *****************************/
        protected IEnumerator Await(AsyncRequest r, Action<float> onProgress = null)
        {
            while (!r.IsDone)
            {
                yield return null;

                if (r.IsError)
                {
                    Debug.LogWarning(r.ErrorMessage);
                    yield break;
                }

                AsyncRequest request = r;
                while (request != null)
                {
                    onProgress?.Invoke(request.ProgressPercent);
                    request = request.CurrentSubrequest;
                }
            }
        }
    }
}
