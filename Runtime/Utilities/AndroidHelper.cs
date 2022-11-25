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
            throw new System.PlatformNotSupportedException("Requires Android Platform.");
#endif
        }
        
        internal static int GetAndroidSDKVersion()
        {
#if PLATFORM_ANDROID
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
#else
            throw new System.PlatformNotSupportedException("Requires Android Platform.");
#endif
        }
    }
}
