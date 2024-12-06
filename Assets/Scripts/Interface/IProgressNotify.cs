using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPlanUtility.AvatarSDK
{
    public interface IProgressNotify
    {
        void SetProgressDelegate(Action<float> progressAction);
    }
}
