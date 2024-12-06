using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using ItSeez3D.AvatarSdk.Core;

using XPlan.Utility;
using XPlan.Net;

namespace XPlanUtility.AvatarSDK
{
    public class LicenseFromAPI : ILicenseGetter
    {
        // Start is called before the first frame update
        public void InitialCredential(Action<bool> finishAction)
		{
            MonoBehaviourHelper.StartCoroutine(InitialCredential_internal(finishAction));
		}

        /*******************************
        * Avatar SDK License
        * *****************************/
        private IEnumerator InitialCredential_internal(Action<bool> finishAction)
        {
            Task<NetParseResult<string>> appIDTask  = RequestAvatarSDKAppID();
            Task<NetParseResult<string>> secretTask = RequestAvatarSDKSecret();

            yield return new WaitUntil(() => appIDTask.IsCompleted);
            yield return new WaitUntil(() => secretTask.IsCompleted);

            if (appIDTask.Result.bSuccess && secretTask.Result.bSuccess)
            {
#if AVATARSDK_CLOUD
                AuthUtils.SetCredentials(appIDTask.Result.GetData(), secretTask.Result.GetData());
#endif // AVATARSDK_CLOUD
                finishAction?.Invoke(false);
            }
            else
            {
                new ErrorNotifyMsg("無法正常取得AvatarSDk的License");

                finishAction?.Invoke(false);
            }
        }

        public async Task<NetParseResult<string>> RequestAvatarSDKAppID()
        {
            NetParseResult<string> response = await HttpHelper.Http.Post(AvatarDefine.BaseUrl + "/SystemInformation/GetSystemInformation")
            .AddQuery("configName", "GreatWigHairAppID")
            .SendAsyncParse<string>();

            return response;
        }

        public async Task<NetParseResult<string>> RequestAvatarSDKSecret()
        {
            NetParseResult<string> response = await HttpHelper.Http.Post(AvatarDefine.BaseUrl + "/SystemInformation/GetSystemInformation")
            .AddQuery("configName", "GreatWigHairSecret")
            .SendAsyncParse<string>();

            return response;
        }
    }
}
