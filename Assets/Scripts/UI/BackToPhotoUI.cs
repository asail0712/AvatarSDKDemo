using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using XPlan.UI;

namespace Granden.Demo
{
    public class BackToPhotoUI : UIBase
    {
		[SerializeField]
		private GameObject uiRoot;

		[SerializeField]
        private Button backToPhotoBtn;

		[SerializeField]
		private Button playMotionBtn;

		private void Awake()
		{
			RegisterButton(UIRequest.BackToPhoto, backToPhotoBtn);
			RegisterButton(UIRequest.PlayMotion, playMotionBtn);
		}
	}
}
