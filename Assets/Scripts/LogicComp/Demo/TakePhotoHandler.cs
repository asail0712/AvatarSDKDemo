using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using XPlan;
using XPlan.Extensions;
using XPlan.Interface;
using XPlan.UI;
using XPlan.Utility;

using Granden.AvatarSDK;

namespace Granden.Demo
{
	public enum MotionType
	{
		blink,
		chewing,
		distrust,
		fear,
		kiss,
		mouth_left_right,
		puff,
		smile,
		yawning,

		NumOfType
	}

	public class TakePhotoHandler : LogicComponentBase
	{
		public TakePhotoHandler(GameObject headAnchor)
		{	
			AddUIListener<Texture2D>(UIRequest.TakePhoto, (photo) =>
			{
				IHeadGenerator headGenerator = ServiceLocator.GetService<IHeadGenerator>();

				if (headGenerator == null)
				{
					Debug.LogError("沒有IHeadGenerator物件");
					return;
				}

				headGenerator.GenerateHead(photo, (headGO) =>
				{
					Debug.Log("成功收到頭部模型");

					// 關閉 Load UI 以及 拍照UI
					ShowModelPage();

					if (headGO == null)
					{
						return;
					}

					// 設定頭部模型位置
					headAnchor.ClearAllChildren();
					headAnchor.AddChild(headGO);

					headAnchor.transform.localPosition		= new Vector3(0f, 1f, 5f);
					headAnchor.transform.localEulerAngles	= new Vector3(5f, 180f, 0f);
					headAnchor.transform.localScale			= new Vector3(35f, 35f, 35f);

					headGO.transform.localPosition			= Vector3.zero;
				});

				// 顯示 Loading UI
				ShowLoadingPage();
			});

			AddUIListener(UIRequest.BackToPhoto, () =>
			{
				// UI 顯示調整
				ShowPhotoPage();

				// 移除原本的頭部
				headAnchor.ClearAllChildren();				
			});

			AddUIListener(UIRequest.PlayMotion, () =>
			{
				// 產生隨機表情
				Animator motionAnimator	= headAnchor.GetComponentInChildren<Animator>();
				MotionType randomType	= (MotionType)Random.Range(0, (int)MotionType.NumOfType);

				if(motionAnimator == null)
				{
					return;
				}

				Debug.Log($"Motion is {randomType.ToString()}");

				motionAnimator.Play(randomType.ToString());
			});

			StartCoroutine(AddProgressDelegate());
			StartCoroutine(AddErrorDelegate());
		}

		protected override void OnDispose(bool bAppQuit)
		{
			if (bAppQuit)
			{
				return;
			}
		}

		/*************************************
		 * 頁面切換
		 * **********************************/
		private void ShowPhotoPage()
		{
			DirectCallUI<bool>(UICommand.ShowTakePhoto, true);
			UIController.Instance.SetUIVisible<BackToPhotoUI>(false);
		}

		private void ShowLoadingPage()
		{
			UIController.Instance.SetUIVisible<LoadingUI>(true);
		}

		private void ShowModelPage()
		{
			// 關閉 Load UI 以及 拍照UI
			DirectCallUI<bool>(UICommand.ShowTakePhoto, false);
			UIController.Instance.SetUIVisible<LoadingUI>(false);

			// 顯示 返回UI
			UIController.Instance.SetUIVisible<BackToPhotoUI>(true);
		}

		/*************************************
		 * 錯誤訊息處理
		 * **********************************/

		private IEnumerator AddErrorDelegate()
		{
			yield return new WaitUntil(() => ServiceLocator.HasService<IErrorNotify>());

			IErrorNotify errorNotify = ServiceLocator.GetService<IErrorNotify>();

			if (errorNotify != null)
			{
				errorNotify.SetErrorDelegate((errorStr) =>
				{
					DirectCallUI<string>(UICommand.ShowError, errorStr);
				});
			}
		}

		/*************************************
		 * 生成進度確認
		 * **********************************/
		private IEnumerator AddProgressDelegate()
		{
			yield return new WaitUntil(() => ServiceLocator.HasService<IProgressNotify>());

			IProgressNotify progressNotify = ServiceLocator.GetService<IProgressNotify>();

			if(progressNotify!= null)
			{
				progressNotify.SetProgressDelegate((currProgress)=> 
				{
					DirectCallUI<float>(UICommand.CurrProgress, currProgress);
				});
			}
		}
	}
}