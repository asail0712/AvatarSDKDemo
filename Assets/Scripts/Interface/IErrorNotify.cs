using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPlanUtility.AvatarSDK
{
    public interface IErrorNotify
    {
        void SetErrorDelegate(Action<string> errorAction);
    }
}
