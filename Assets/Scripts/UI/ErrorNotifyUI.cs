using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using XPlan.UI;

namespace Granden.Demo
{
    public class ErrorNotifyUI : UIBase
    {
		[SerializeField]
		private Text errorTxt;

		private Coroutine showErrorCoroutine = null;

		private void Awake()
		{
			errorTxt.text = "";

			ListenCall<string>(UICommand.ShowError, (errorStr) => 
			{
				if(showErrorCoroutine != null)
				{
					StopCoroutine(showErrorCoroutine);
					showErrorCoroutine = null;
				}

				showErrorCoroutine = StartCoroutine(ShowErrorInfo(errorStr));
			});
		}

		private IEnumerator ShowErrorInfo(string errorStr)
		{
			errorTxt.text = errorStr;

			yield return new WaitForSeconds(5f);

			errorTxt.text		= "";
			showErrorCoroutine	= null;
		}
	}
}
