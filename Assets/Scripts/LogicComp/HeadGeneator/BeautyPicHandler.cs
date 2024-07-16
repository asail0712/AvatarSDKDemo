using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using Newtonsoft.Json;

using XPlan;
using XPlan.Extensions;
using XPlan.Observe;
using XPlan.Utility;

namespace Granden.AvatarSDK
{
    public class PhotoImageMsg : MessageBase
    {
        public Texture2D photoTexture;
        public Action<GameObject> finishAction;

        public PhotoImageMsg(Texture2D photoTexture, Action<GameObject> finishAction)
        {
            this.photoTexture = photoTexture;
            this.finishAction = finishAction;
        }
    }

    public class BeautyPicHandler : LogicComponentBase
    {
        private string meituApiKey      = "";
        private string meituSecretID    = "";

        public BeautyPicHandler(bool bNeedToBeauty)
        {
            TextAsset textAsset     = Resources.Load<TextAsset>("Meitu/License");
            bool bFindLicense       = false;

            if (textAsset != null)
            {
                // 取得文字檔內容
                string textContent      = textAsset.text;
                string[] textContentArr = textContent.Split(",");

                Debug.Log(textContent);

                if (textContentArr.Length == 2)
				{
                    meituApiKey     = textContentArr[0];
                    meituSecretID   = textContentArr[1];
                    bFindLicense    = true;
                }
            }

            if(!bFindLicense)
            {
                bNeedToBeauty = false;
                Debug.LogWarning("未能成功取出 Meitu License。");
            }

            RegisterNotify<PhotoImageMsg>(this, (msg) => 
            {
                if (bNeedToBeauty)
                {
                    // 美顏有分兩個流程
                    BeautyPhotoFlow1(msg.photoTexture.TexToBase64(), (beautyPhoto1) =>
                    {
                        Texture2D newBeautyPhoto = beautyPhoto1 == null ? msg.photoTexture : beautyPhoto1;

                        SendMsg<ProgressNotifyMsg>(0.15f);

                        BeautyPhotoFlow2(newBeautyPhoto.TexToBase64(), (beautyPhoto2) =>
                        {
                            newBeautyPhoto = beautyPhoto2 == null ? newBeautyPhoto : beautyPhoto2;

                            SendMsg<ProgressNotifyMsg>(0.3f);
                            SendMsg<GenerateHeadMsg>(newBeautyPhoto, msg.finishAction);
                        });
                    });
                }
                else
                {
                    SendMsg<GenerateHeadMsg>(msg.photoTexture, msg.finishAction);
                }
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
        * Beauty流程
        * *****************************/

        private void BeautyPhotoFlow1(string base64Img, Action<Texture2D> onFinsh)
        {
            LivePhotoRequest livePhotoRequest               = new LivePhotoRequest();
            livePhotoRequest.media_info_list[0]             = new LivePhotoRequestMediaInfo();
            livePhotoRequest.media_info_list[0].media_data  = base64Img;

            // 將 C# 對象轉換為 JSON 字符串
            string jsonData = JsonConvert.SerializeObject(livePhotoRequest);
            byte[] bodyRaw  = System.Text.Encoding.UTF8.GetBytes(jsonData);

            // 指定目標 URL
            string url = MeituDefine.BeautyLivePhotoUrl + "?api_key=" + meituApiKey + "&api_secret=" + meituSecretID;

            // 發送請求
            StartCoroutine(SendRequest(url, bodyRaw, onFinsh));
        }

        private void BeautyPhotoFlow2(string base64Img, Action<Texture2D> onFinsh)
        {
            JSDNData jsdnData                           = new JSDNData();
            jsdnData.parameter                          = new Parameter(30001, 100, 50, "jpg");
            jsdnData.extra                              = new Extra();
            jsdnData.media_info_list                    = new MediaInfo[1];
            jsdnData.media_info_list[0]                 = new MediaInfo();
            jsdnData.media_info_list[0].media_data      = base64Img;
            jsdnData.media_info_list[0].media_profiles  = new MediaProfiles();


            // 將 C# 對象轉換為 JSON 字符串
            string jsonData = JsonConvert.SerializeObject(jsdnData);
            byte[] bodyRaw  = System.Text.Encoding.UTF8.GetBytes(jsonData);

            // 指定目標 URL
            string url = MeituDefine.OldBeautyRequestUrl + "?api_key=" + meituApiKey + "&api_secret=" + meituSecretID;

            // 發送請求
            StartCoroutine(SendRequest(url, bodyRaw, onFinsh));
        }

        IEnumerator SendRequest(string url, byte[] bodyRaw, Action<Texture2D> onFinsh)
        {
            // 創建 UnityWebRequest 對象
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                // 設置 request 的屬性
                request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // 發送請求並等待回應
                yield return request.SendWebRequest();

                string jsonResponse = request.downloadHandler.text;

                // 檢查是否有錯誤
                if (request.result == UnityWebRequest.Result.Success)
                {
                    // 處理成功的回應
                    Debug.Log("Response: " + request.downloadHandler.text);

                    LivePhotoResponseSuccess response = JsonConvert.DeserializeObject<LivePhotoResponseSuccess>(jsonResponse);

                    onFinsh?.Invoke(response.media_info_list[0].media_data.Base64ToTex());

                    yield break;
                }
                else
				{
                    SendMsg<ErrorNotifyMsg>($"API美顏 {url} 調用失敗", ErrorLevel.Warning);

                    onFinsh?.Invoke(null);
                }
            }
        }
    }
}
