using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using XPlan;
using XPlan.Observe;
using XPlan.Utility;

namespace XPlanUtility.AvatarSDK
{
	public class HeadGeneratorHandler : LogicComponent, IHeadGenerator
    {        
        public HeadGeneratorHandler()
        {
            ServiceLocator.Register<IHeadGenerator>(this);
        }

		protected override void OnDispose(bool bAppQuit)
		{
            ServiceLocator.Deregister<IHeadGenerator>();

            if (bAppQuit)
			{
                return;
			}
        }

        /*******************************
         * 實作IHeadGenerator
         * *****************************/
        public void GenerateHead(Texture2D imgTexture, Action<GameObject> finsihAction)
        {
            // 要求進行拍照
            SendMsg<PhotoImageMsg>(imgTexture, finsihAction);
        }
    }
}
