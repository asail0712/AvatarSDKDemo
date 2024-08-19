using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using XPlan;

namespace Granden.Demo
{
    public class TakePhotoSystem : SystemBase
    {
		[SerializeField]
		private GameObject headAnchor;

		protected override void OnInitialGameObject()
		{
			Application.targetFrameRate = 60;
		}

		protected override void OnInitialHandler()
		{
			RegisterLogic(new TakePhotoHandler(headAnchor));
		}
	}
}
