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
    public enum ErrorLevel
	{
        Warning = 0,
        Error,
	}

    public class ProgressNotifyMsg : MessageBase
	{
        public float currProgress;

        public ProgressNotifyMsg(float currProgress)
		{
            this.currProgress = currProgress;
		}
    }

    public class ErrorNotifyMsg : MessageBase
    {
        public string errorStr;
        public ErrorLevel errorLevel;

        public ErrorNotifyMsg(string errorStr, ErrorLevel errorLevel = ErrorLevel.Error)
        {
            this.errorStr   = errorStr;
            this.errorLevel = errorLevel;
        }
    }

    public class StatusNotifyHandler : LogicComponent, IErrorNotify, IProgressNotify
    {
        private Action<float> progressAction;
        private Action<string> errorAction;

        public StatusNotifyHandler()
        {
            ServiceLocator.Register<IErrorNotify>(this);
            ServiceLocator.Register<IProgressNotify>(this);

            RegisterNotify<ErrorNotifyMsg>((msg)=> 
            {
                switch (msg.errorLevel)
                {
                    case ErrorLevel.Error:
                        Debug.LogError(msg.errorStr);
                        break;
                    case ErrorLevel.Warning:
                        Debug.LogWarning(msg.errorStr);
                        break;
                }

                if(msg.errorLevel == ErrorLevel.Error)
				{
                    errorAction?.Invoke(msg.errorStr);
                }                
            });

            RegisterNotify<ProgressNotifyMsg>((msg) =>
            {
                progressAction?.Invoke(msg.currProgress);
            });
        }

        protected override void OnDispose(bool bAppQuit)
        {
            ServiceLocator.Deregister<IErrorNotify>();
            ServiceLocator.Deregister<IProgressNotify>();

            if (bAppQuit)
            {
                return;
            }

            errorAction     = null;
            progressAction  = null;
        }

        /*******************************
        * IErrorNotify
        * *****************************/
        public void SetErrorDelegate(Action<string> errorAction)
		{
            this.errorAction += errorAction;
        }

        /*******************************
        * IProgressNotify
        * *****************************/
        public void SetProgressDelegate(Action<float> progressAction)
		{
            this.progressAction += progressAction;
        }
    }
}
