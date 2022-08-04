using UnityEngine;

namespace Signalboy
{
    internal class AndroidHelper
    {
        internal static AndroidJavaObject GetCurrentActivity()
        {
            using (
                AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
            )
            {
                return jc.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }
    }
}
