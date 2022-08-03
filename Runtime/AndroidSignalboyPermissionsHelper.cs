using Signalboy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

namespace Signalboy.Utilities
{
    public static class AndroidSignalboyPermissionsHelper
    {
        private static List<string> locationRuntimePermissions = new List<string> {
        Permission.FineLocation,
    };
        private static List<string> bluetoothRuntimePermissions = new List<string>
    {
        PermissionAPI31.BluetoothConnect,
        PermissionAPI31.BluetoothScan,
    };

        public static async Task<bool> RequestRuntimePermissionsAsync()
        {
            if (Application.isEditor)
            {
                throw new PlatformNotSupportedException();
            }

#if PLATFORM_ANDROID
            List<string> requiredRuntimePermissions;
            if (getAndroidSDKVersion() >= 31)
            {
                requiredRuntimePermissions = bluetoothRuntimePermissions;
            }
            else
            {
                requiredRuntimePermissions = locationRuntimePermissions;
            }

            if (requiredRuntimePermissions.Any(permission => !Permission.HasUserAuthorizedPermission(permission)))
            {
                var results = await RequestUserPermissionsAsync(requiredRuntimePermissions.ToArray());
                Debug.Log($"Finished awaiting requested runtime-permissions. (results={results})");
                return isEachPermissionGranted(results);
            }
            else
            {
                return true;
            }
#endif
        }

        #region Private
        private static bool isEachPermissionGranted(Dictionary<string, PermissionCallbackResult> results) =>
            results.Values.All(result => result == PermissionCallbackResult.Granted);

        private static async Task<Dictionary<string, PermissionCallbackResult>> RequestUserPermissionsAsync(string[] permissions)
        {
            var tcs = makeTaskCompletionSource();
            try
            {
                Permission.RequestUserPermissions(permissions, makePermissionCallbacks(permissions, results =>
                    tcs.TrySetResult(results)
                ));
            }
            catch (Exception err)
            {
                tcs.SetException(err);
            }

            return await tcs.Task;
        }

        private static TaskCompletionSource<Dictionary<string, PermissionCallbackResult>> makeTaskCompletionSource() =>
            new TaskCompletionSource<Dictionary<string, PermissionCallbackResult>>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

        private static PermissionCallbacks makePermissionCallbacks(string[] permissionsToAwait, Action<Dictionary<string, PermissionCallbackResult>> completion)
        {
            var results = new Dictionary<string, PermissionCallbackResult>();
            Action<string, PermissionCallbackResult> onCallbackResult = (permission, result) =>
            {
                results.Add(permission, result);
                if (permissionsToAwait.All(permission => results.ContainsKey(permission)))
                {
                    // Received callback for all requested `permissions`.
                    completion(results);
                }
            };

            var callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += permission =>
            {
                Debug.LogWarning($"Permission request for runtime permission ({permission}) has been denied.");
                onCallbackResult(permission, PermissionCallbackResult.Denied);
            };
            callbacks.PermissionGranted += permission =>
            {
                Debug.Log($"Permission request for runtime permission ({permission}) has been granted.");
                onCallbackResult(permission, PermissionCallbackResult.Granted);
            };
            callbacks.PermissionDeniedAndDontAskAgain += permission =>
            {
                Debug.LogWarning($"Permission request for runtime permission ({permission}) has been denied and been marked with \"don't ask again\".");
                onCallbackResult(permission, PermissionCallbackResult.DeniedAndDontAskAgain);
            };
            return callbacks;
        }

        private static int getAndroidSDKVersion()
        {
#if PLATFORM_ANDROID
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
#else
            throw System.PlatformNotSupportedException();
#endif
        }

        private struct PermissionAPI31
        {
            public const string BluetoothScan = "android.permission.BLUETOOTH_SCAN";
            public const string BluetoothConnect = "android.permission.BLUETOOTH_CONNECT";
            public const string BluetoothAdvertise = "android.permission.BLUETOOTH_ADVERTISE";
        }

        private enum PermissionCallbackResult
        {
            Denied,
            Granted,
            DeniedAndDontAskAgain
        }
        #endregion
    }
}
