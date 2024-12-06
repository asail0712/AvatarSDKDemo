using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

using ItSeez3D.AvatarSdk.Core;

using XPlan.Utility;
using XPlan.Net;

namespace XPlanUtility.AvatarSDK
{
    public class LicenseFromFile : ILicenseGetter
    {
        private string licensePath;

        public LicenseFromFile(string licensePath)
		{
            this.licensePath = licensePath;
        }

        // Start is called before the first frame update
        public void InitialCredential(Action<bool> finishAction)
		{
#if AVATARSDK_CLOUD
            if(File.Exists(licensePath))
			{
                string licenseStr       = File.ReadAllText(licensePath);
                string[] licenseContent = licenseStr.Split(" ");

                if(licenseContent.Length == 2)
				{
                    AuthUtils.SetCredentials(licenseContent[0], licenseContent[1]);

                    finishAction?.Invoke(true);
                }
            }
            
            finishAction?.Invoke(false);
#elif AVATARSDK_LOCAL
            finishAction?.Invoke(false);
#endif // AVATARSDK_CLOUD
		}
    }
}
