using System.Collections.Generic;
using UnityEngine;

namespace Signalboy
{
    internal class AndroidJNICollectionHelper
    {
        internal static List<T> ConvertFromAndroidJavaList<T>(AndroidJavaObject androidJavaList)
        {
            var array = AndroidJNIHelper.ConvertFromJNIArray<T[]>(androidJavaList.Call<AndroidJavaObject>("toArray")
                .GetRawObject());
            return new List<T>(array);
        }
    }
}
