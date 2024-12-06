using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XPlanUtility.AvatarSDK
{
    public interface ILicenseGetter
    {
        void InitialCredential(Action<bool> finishAction);
    }
}
