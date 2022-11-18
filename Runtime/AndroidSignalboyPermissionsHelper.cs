using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private static readonly List<string> LocationRuntimePermissions = new()
        {
#if PLATFORM_ANDROID
            Permission.FineLocation,
#endif
        };

        private static readonly List<string> BluetoothRuntimePermissions = new()
        {
#if PLATFORM_ANDROID
            PermissionAPI31.BluetoothConnect,
            PermissionAPI31.BluetoothScan,
#endif
        };

        public static async Task<bool> RequestRuntimePermissionsAsync()
        {
            if (Application.isEditor) throw new PlatformNotSupportedException();

#if PLATFORM_ANDROID
            List<string> requiredRuntimePermissions;
            if (GetAndroidSDKVersion() >= 31)
                requiredRuntimePermissions = BluetoothRuntimePermissions;
            else
                requiredRuntimePermissions = LocationRuntimePermissions;

            if (requiredRuntimePermissions.Any(permission => !Permission.HasUserAuthorizedPermission(permission)))
            {
                var results = await RequestUserPermissionsAsync(requiredRuntimePermissions.ToArray());
                Debug.Log($"Finished awaiting requested runtime-permissions. (results={results})");
                return IsEachPermissionGranted(results);
            }

            return true;
#else
            throw new PlatformNotSupportedException();
#endif
        }

        #region Private

        private static bool IsEachPermissionGranted(Dictionary<string, PermissionCallbackResult> results)
        {
            return results.Values.All(result => result == PermissionCallbackResult.Granted);
        }

        private static async Task<Dictionary<string, PermissionCallbackResult>> RequestUserPermissionsAsync(
            string[] permissions)
        {
            var tcs = MakeTaskCompletionSource();
            try
            {
                // `PermissionCallbacks` is only available on Android (UnityEngine.Android)
#if PLATFORM_ANDROID
                Permission.RequestUserPermissions(permissions, MakePermissionCallbacks(permissions, results =>
                    tcs.TrySetResult(results)
                ));
#else
                throw new PlatformNotSupportedException();
#endif
            }
            catch (Exception err)
            {
                tcs.SetException(err);
            }

            return await tcs.Task;
        }

        private static TaskCompletionSource<Dictionary<string, PermissionCallbackResult>> MakeTaskCompletionSource()
        {
            return new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
        }

#if PLATFORM_ANDROID
        private static PermissionCallbacks MakePermissionCallbacks(string[] permissionsToAwait,
            Action<Dictionary<string, PermissionCallbackResult>> completion)
        {
            var results = new Dictionary<string, PermissionCallbackResult>();

            void OnCallbackResult(string permission, PermissionCallbackResult result)
            {
                results.Add(permission, result);
                if (permissionsToAwait.All(permissionToAwait => results.ContainsKey(permissionToAwait)))
                    // Received callback for all requested `permissions`.
                    completion(results);
            }

            var callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += permission =>
            {
                Debug.LogWarning($"Permission request for runtime permission ({permission}) has been denied.");
                OnCallbackResult(permission, PermissionCallbackResult.Denied);
            };
            callbacks.PermissionGranted += permission =>
            {
                Debug.Log($"Permission request for runtime permission ({permission}) has been granted.");
                OnCallbackResult(permission, PermissionCallbackResult.Granted);
            };
            callbacks.PermissionDeniedAndDontAskAgain += permission =>
            {
                Debug.LogWarning(
                    $"Permission request for runtime permission ({permission}) has been denied and been marked with \"don't ask again\".");
                OnCallbackResult(permission, PermissionCallbackResult.DeniedAndDontAskAgain);
            };
            return callbacks;
        }
#endif

        private static int GetAndroidSDKVersion()
        {
#if PLATFORM_ANDROID
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
#else
            throw new System.PlatformNotSupportedException();
#endif
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private struct PermissionAPI31
        {
#if PLATFORM_ANDROID
            public const string BluetoothScan = "android.permission.BLUETOOTH_SCAN";
            public const string BluetoothConnect = "android.permission.BLUETOOTH_CONNECT";
            public const string BluetoothAdvertise = "android.permission.BLUETOOTH_ADVERTISE";
#endif
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