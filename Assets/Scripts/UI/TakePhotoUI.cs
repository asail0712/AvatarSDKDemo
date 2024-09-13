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

			WebCamController webCamController = WebCamUtility.GenerateCamController(cameraImg, true);
			webCamController.Play();
			//WebCamTexture webCamTexture = WebCamUtility.FindWebCamTexture();

			//         if (webCamTexture == null)
			//         {
			//             new ErrorNotifyMsg(ErrorDefine.EXTERNAL_CAMERA_REQUIRED);

			//             yield break;
			//         }

			//WebCamUtility.InitialCameraImg(cameraImg, webCamTexture);
		}
	}
}
