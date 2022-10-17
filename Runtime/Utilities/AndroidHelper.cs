using System;
using UnityEngine;

namespace Signalboy
{
    internal class AndroidHelper
    {
        internal static AndroidJavaObject GetCurrentActivity()
        {
#if PLATFORM_ANDROID
            using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                return jc.GetStatic<AndroidJavaObject>("currentActivity");
            }
#else
            throw new PlatformNotSupportedException("Requires Android Platform.");
#endif
        }
    }
}
