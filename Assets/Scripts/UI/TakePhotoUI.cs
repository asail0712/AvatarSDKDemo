using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

using XPlan.UI;
using XPlan.Utility;

using Granden.AvatarSDK;

namespace Granden.Demo
{
	public class TakePhotoUI : UIBase
	{
		[SerializeField]
		private GameObject uiRoot;

		[SerializeField]
		private Button takePhotoBtn;

		[SerializeField]
		private RawImage cameraImg;

        [SerializeField]
        private string defaultCameraName = "HD,Real";

        private void Awake()
		{
			RegisterButton("", takePhotoBtn, () => 
			{
				WebCamTexture webcamTexture = (WebCamTexture)cameraImg.texture;
				Texture2D texture2D			= new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
				texture2D.SetPixels32(webcamTexture.GetPixels32());
				texture2D.Apply();

				DirectTrigger<Texture2D>(UIRequest.TakePhoto, texture2D);
			});

			/**************************
			* UI Listener
			**************************/
			ListenCall<bool>(UICommand.ShowTakePhoto, (bNeedToShow) => 
            {
                uiRoot.SetActive(bNeedToShow);
            });

			/**************************
            * Camera初始化
            **************************/
			StartCoroutine(InitialWebCamera(cameraImg, defaultCameraName));
		}

        /*******************************
         * Web Camera相關
         * *****************************/
        private IEnumerator InitialWebCamera(RawImage cameraImg, string cameraName)
        {
            if (cameraImg == null)
            {
                yield break;
            }

            WebCamDevice[] deviceList = WebCamTexture.devices;
            bool bFindCamera = false;

            if (deviceList.Length == 0)
            {
                new ErrorNotifyMsg(ErrorDefine.EXTERNAL_CAMERA_REQUIRED);

                yield break;
            }

            if (string.Empty != cameraName)
            {
                string[] cameraNameList = cameraName.Split(",");

                for (int i = 0; i < deviceList.Length; ++i)
                {
                    for (int j = 0; j < cameraNameList.Length; ++j)
                    {
                        if (deviceList[i].name.Contains(cameraNameList[j]))
                        {
                            ChooseCameraDevice(cameraImg, i);
                            bFindCamera = true;
                            break;
                        }
                    }
                }
            }

            if (!bFindCamera)
            {
                ChooseCameraDevice(cameraImg, 0);
                Debug.LogWarning("沒找到面向玩家的鏡頭");
            }
        }

        private void ChooseCameraDevice(RawImage cameraImg, int idx)
        {
            WebCamDevice[] deviceList = WebCamTexture.devices;

            int deviceIdx = Mathf.Clamp(idx, 0, deviceList.Length - 1);

            // 重新生成webcamTexture
            if (cameraImg.texture != null)
            {
                cameraImg.enabled = false;
                ((WebCamTexture)cameraImg.texture).Stop();
                GameObject.Destroy(cameraImg.texture);
                cameraImg.texture = null;
            }

            WebCamTexture webcamTexture = new WebCamTexture(deviceList[deviceIdx].name);

            cameraImg.texture = webcamTexture;
            cameraImg.enabled = true;

            Debug.Log($"choose camera to {deviceList[deviceIdx].name}");

            webcamTexture.Play();

            MonoBehaviourHelper.StartCoroutine(WaitCameraDeviceInitial(cameraImg, webcamTexture));
        }


        private IEnumerator WaitCameraDeviceInitial(RawImage cameraImg, WebCamTexture webcamTexture)
        {
            // 參考資料
            // https://blog.csdn.net/jingxuan2583/article/details/99709387
            // https://forum.unity.com/threads/webcamtexture-always-return-16-for-width-and-height.327015/

            if (webcamTexture.width <= 16)
            {
                Debug.Log($"webcamTexture need to initial !!");

                while (!webcamTexture.didUpdateThisFrame)
                {
                    yield return new WaitForEndOfFrame();
                }

                Debug.Log($"webcamTexture Update Frame !!");
            }

            Debug.Log($"webcamTexture initial complete !!");

            // 先調整Img大小
            FitImageSizeToCamSize(cameraImg, webcamTexture);
        }

        private void FitImageSizeToCamSize(RawImage cameraImg, WebCamTexture webcamTexture)
        {
            AspectRatioFitter ratioFitter = cameraImg.GetComponent<AspectRatioFitter>();

            if (ratioFitter != null)
            {
                // 確認螢幕是否需要旋轉
                float aspectRatio       = (float)webcamTexture.width / (float)webcamTexture.height;
                ratioFitter.aspectRatio = aspectRatio;
                ratioFitter.aspectMode  = AspectRatioFitter.AspectMode.HeightControlsWidth;
            }

            Debug.Log($"cameraImg width : {cameraImg.mainTexture.width}");
            Debug.Log($"cameraImg height : {cameraImg.mainTexture.height}");

            Debug.Log($"Web width : {webcamTexture.width}");
            Debug.Log($"Web height : {webcamTexture.height}");
            Debug.Log($"WebcamTexture: {webcamTexture.width} {webcamTexture.height}");
            Debug.Log($"WebCamTexture.FPS:{webcamTexture.requestedFPS}");
        }
	}
}
