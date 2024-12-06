using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XPlanUtility.AvatarSDK
{
    public interface IHeadGenerator
    {
        void GenerateHead(Texture2D imgTexture, Action<GameObject> finsihAction);
    }
}
