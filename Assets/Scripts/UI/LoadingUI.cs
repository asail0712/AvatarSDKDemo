using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using XPlan.UI;

namespace XPlanUtility.AvatarSDK
{
    public class LoadingUI : UIBase
    {
		[SerializeField]
		private GameObject uiRoot;

		[SerializeField]
		private Slider progressSlider;

		private const float LerpThredhold = 0.01f;

		private float targetProgress;

		private void Awake()
		{
			ListenCall<float>(UICommand.CurrProgress, (f) => 
			{
				targetProgress = f;
			});
		}

		private void Update()
		{
			if(targetProgress - progressSlider.value < 0.01f)
			{
				return;
			}

			float currProgress		= progressSlider.value;
			float lerpProgress		= currProgress + (targetProgress - currProgress) * LerpThredhold;
			progressSlider.value	= lerpProgress;
		}

		private void OnEnable()
		{
			progressSlider.value	= 0f;
			targetProgress			= 0.1f;
		}

		private void OnDisable()
		{
			progressSlider.value	= 0f;
			targetProgress			= 0f;
		}
	}
}
