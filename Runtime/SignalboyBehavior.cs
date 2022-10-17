using Signalboy.Wrappers;
using System;
using UnityEngine;

#nullable enable

namespace Signalboy
{
    public class SignalboyBehavior : MonoBehaviour
    {
        public State? state { get => signalboyFacade?.state; }
        public ConnectionStateUpdateCallback? connectionStateUpdateCallback;

        // Not-null when Android-Service has been bound successfully.
        private SignalboyFacadeWrapper? signalboyFacade;

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

        public SignalboyFacadeWrapper.PrerequisitesResult VerifyPrerequisites()
        {
            using (var context = AndroidHelper.GetCurrentActivity())
            {
                var bluetoothAdapter = SignalboyFacadeWrapper.GetDefaultAdapter(context);
                return SignalboyFacadeWrapper.VerifyPrerequisites(context, bluetoothAdapter);
            }
        }

        // convenience method
        public void BindService()
        {
            BindService(SignalboyFacadeWrapper.Configuration.Default);
        }

        public void BindService(SignalboyFacadeWrapper.Configuration configuration)
        {
            using (var context = AndroidHelper.GetCurrentActivity().Call<AndroidJavaObject>("getApplicationContext"))
            {
                var intent = new AndroidJavaObject("android.content.Intent");
                using (var componentName = new AndroidJavaObject("android.content.ComponentName", context, SignalboyFacadeWrapper.CLASSNAME))
                {
                    intent = intent.Call<AndroidJavaObject>("setComponent", componentName);
                }
                intent = intent.Call<AndroidJavaObject>("putExtra", SignalboyFacadeWrapper.EXTRA_CONFIGURATION, configuration.GetJavaInstance());

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
            if (signalboyFacade != null)
            {
                signalboyFacade.SendEvent();
            }
        }

        public void _DebugTriggerSync()
        {
            if (signalboyFacade != null)
            {
                signalboyFacade.TryTriggerSync();
            }
        }

        private class ServiceConnection : AndroidJavaProxy
        {
            private SignalboyBehavior parent;

            internal ServiceConnection(SignalboyBehavior parent) : base("android.content.ServiceConnection")
            {
                this.parent = parent;
            }

            private void onServiceConnected(AndroidJavaObject componentName, AndroidJavaObject service)
            {
                // service: SignalboyFacade.LocalBinder
                var signalboyFacade = new SignalboyFacadeWrapper(service.Call<AndroidJavaObject>("getService"));
                signalboyFacade.SetOnConnectionStateUpdateListener(
                    new ConnectionStateUpdateListener(connectionState =>
                        parent.connectionStateUpdateCallback?.Invoke(connectionState))
                );

                parent.signalboyFacade = signalboyFacade;
            }

            private void onServiceDisconnected(AndroidJavaObject componentName)
            {
                // This is called when the connection with the service has been
                // unexpectedly disconnected -- that is, its process crashed.
                Debug.LogError("onServiceDisconnected");
                parent.signalboyFacade = null;
            }
        }
    }

    public delegate void ConnectionStateUpdateCallback(State connectionState);
}
