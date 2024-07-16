using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using XPlan.UI;

namespace Granden.Demo
{
    public class WaitingUI : UIBase
    {
		[SerializeField]
		private GameObject uiRoot;

		private void Awake()
		{
			ListenCall<bool>(UICommand.ShowTakePhoto, (b) => 
			{
				uiRoot.SetActive(b);
			});
		}
	}
}
