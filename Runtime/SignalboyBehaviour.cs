using Signalboy.Wrappers;
using System;
using UnityEngine;

#nullable enable

namespace Signalboy
{
    public class SignalboyBehaviour : MonoBehaviour
    {
        public State? State => signalboyService?.State;
        public ConnectionStateUpdateCallback? ConnectionStateUpdateCallback;

        // Not-null when Android-Service has been bound successfully.
        private SignalboyServiceWrapper? signalboyService;

        #region MonoBehaviour - lifecycle
        void Start()
        {
            if (Application.isEditor)
            {
                Debug.Log("Running in editor: Signalboy-Service is not available.");
                return;
            }
#if PLATFORM_ANDROID
        // AndroidJNIHelper.debug = true;
#else
            Debug.LogError("Signalboy requires Android Platform.");
#endif
        }

        void Update()
        {

        }
        #endregion

        public SignalboyServiceWrapper.PrerequisitesResult VerifyPrerequisites()
        {
            using (var context = AndroidHelper.GetCurrentActivity())
            {
                var bluetoothAdapter = SignalboyServiceWrapper.GetDefaultAdapter(context);
                return SignalboyServiceWrapper.VerifyPrerequisites(context, bluetoothAdapter);
            }
        }

        // convenience method
        public void BindService()
        {
            BindService(SignalboyServiceWrapper.Configuration.Default);
        }

        public void BindService(SignalboyServiceWrapper.Configuration configuration)
        {
            using (var context = AndroidHelper.GetCurrentActivity().Call<AndroidJavaObject>("getApplicationContext"))
            {
                var intent = new AndroidJavaObject("android.content.Intent");
                using (var componentName = new AndroidJavaObject("android.content.ComponentName", context, SignalboyServiceWrapper.CLASSNAME))
                {
                    intent = intent.Call<AndroidJavaObject>("setComponent", componentName);
                }
                intent = intent.Call<AndroidJavaObject>("putExtra", SignalboyServiceWrapper.EXTRA_CONFIGURATION, configuration.GetJavaInstance());

                var isSuccess = context.Call<bool>(
                    "bindService",
                    intent,
                    new ServiceConnection(this),
                    1   // android.content.Context#BIND_AUTO_CREATE
                );
                System.Diagnostics.Trace.Assert(
                    isSuccess,
                    "Failed to bind service.",
                    "Package Manager either was unable to find package or security requirements have not been met."
                );
            }
        }

        public void SendEvent()
        {
            if (signalboyService != null)
            {
                signalboyService.TrySendEvent();
            }
        }

        public void _DebugTriggerSync()
        {
            if (signalboyService != null)
            {
                signalboyService.TryTriggerSync();
            }
        }

        private class ServiceConnection : AndroidJavaProxy
        {
            private SignalboyBehaviour parent;

            internal ServiceConnection(SignalboyBehaviour parent) : base("android.content.ServiceConnection")
            {
                this.parent = parent;
            }

            private void onServiceConnected(AndroidJavaObject componentName, AndroidJavaObject service)
            {
                // service: SignalboyService.LocalBinder
                var signalboyService = new SignalboyServiceWrapper(service.Call<AndroidJavaObject>("getService"));
                signalboyService.SetOnConnectionStateUpdateListener(
                    new ConnectionStateUpdateListener(connectionState =>
                        parent.ConnectionStateUpdateCallback?.Invoke(connectionState))
                );

                parent.signalboyService = signalboyService;
            }

            private void onServiceDisconnected(AndroidJavaObject componentName)
            {
                // This is called when the connection with the service has been
                // unexpectedly disconnected -- that is, its process crashed.
                Debug.LogError("onServiceDisconnected");
                parent.signalboyService = null;
            }
        }
    }

    public delegate void ConnectionStateUpdateCallback(State connectionState);
}
