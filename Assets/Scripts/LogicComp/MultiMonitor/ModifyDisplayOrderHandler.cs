using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

using XPlan;
using XPlan.Extensions;
using XPlan.Utility;

namespace Granden.MultiDisplay
{
	// 參考資料
	// https://zxxcc0001.pixnet.net/blog/post/243195373
	// https://blog.csdn.net/BillCYJ/article/details/99712313

	public class ModifyDisplayOrderHandler : LogicComponentBase
	{
		private const string DisplayOrderFileName	= "c:\\Kingstore\\displayOrder.txt";
		private const string DisplayName			= "Camera";
		private const int MaxCameraNum				= 4;

		// Start is called before the first frame update
		public ModifyDisplayOrderHandler()
        {
			StartCoroutine(AllCamera());
		}

		private IEnumerator AllCamera()
		{
			List<Camera> cameraList = new List<Camera>();

			// 確認是不是找到了對應四個螢幕的四個Camera
			while (cameraList.Count < MaxCameraNum)
			{
				cameraList.Clear();

				yield return null;

				Camera[] allCameras = Camera.allCameras;

				for (int i = 0; i < MaxCameraNum; ++i)
				{
					string compareName = DisplayName + (i + 1).ToString();

					int idx = Array.FindIndex<Camera>(allCameras, (camera) =>
					{
						return camera.name.Contains(compareName);
					});

					if (idx == -1)
					{
						continue;
					}

					cameraList.Add(allCameras[idx]);
				}
			}

			// 對Camera對應的Display做修改
			yield return ReadDisplayOrder((orderStr) =>
			{
				string[] orderArr	= orderStr.Split(",");
				int len				= Math.Min(orderArr.Length, MaxCameraNum);
				int outDisplayIdx;

				for (int i = 0; i < len; ++i)
				{
					if ("" == orderArr[i])
					{
						continue;
					}

					if (int.TryParse(orderArr[i], out outDisplayIdx))
					{
						cameraList[i].targetDisplay = outDisplayIdx - 1;
					}
					else
					{
						Debug.LogError($"display order的內容 {orderArr[i]} 無法解譯");
					}
				}

				// 啟動所有使用到的Display
				for (int i = 0; i < Display.displays.Length; ++i)
				{
					Display.displays[i].Activate();
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

		private IEnumerator ReadDisplayOrder(Action<string> finishAction)
		{
			string url				= new Uri(DisplayOrderFileName).AbsoluteUri;
			UnityWebRequest request = UnityWebRequest.Get(url);
			request.downloadHandler = new DownloadHandlerBuffer();

			yield return request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success)
			{
				finishAction?.Invoke("");

				yield break;
			}

			finishAction?.Invoke(request.downloadHandler.text);
		}
	}
}
