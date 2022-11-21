#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Signalboy.Wrappers;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Signalboy
{
    public class SignalboyBehaviour : MonoBehaviour
    {
        // Not-null when Android-Service has been bound successfully.
        private SignalboyServiceWrapper? _signalboyService;
        public ConnectionStateUpdateCallback? ConnectionStateUpdateCallback;
        public State? State => _signalboyService?.State;

        private TaskFactory _uiThreadTaskFactory = null!;

        public SignalboyServiceWrapper.PrerequisitesResult VerifyPrerequisites()
        {
            using var context = AndroidHelper.GetCurrentActivity();
            var bluetoothAdapter = SignalboyServiceWrapper.GetDefaultAdapter(context);
            return SignalboyServiceWrapper.VerifyPrerequisites(context, bluetoothAdapter);
        }

        // convenience method
        public void BindService()
        {
            BindService(SignalboyServiceWrapper.Configuration.Default);
        }

        public void BindService(SignalboyServiceWrapper.Configuration configuration)
        {
            using var context = AndroidHelper.GetCurrentActivity().Call<AndroidJavaObject>("getApplicationContext");
            var intent = new AndroidJavaObject("android.content.Intent");
            using (var componentName = new AndroidJavaObject("android.content.ComponentName", context,
                       SignalboyServiceWrapper.CLASSNAME))
            {
                intent = intent.Call<AndroidJavaObject>("setComponent", componentName);
            }

            intent = intent.Call<AndroidJavaObject>("putExtra", SignalboyServiceWrapper.EXTRA_CONFIGURATION,
                configuration.GetJavaInstance());

            var isSuccess = context.Call<bool>(
                "bindService",
                intent,
                new ServiceConnection(this),
                1 // android.content.Context#BIND_AUTO_CREATE
            );
            Trace.Assert(
                isSuccess,
                "Failed to bind service.",
                "Package Manager either was unable to find package or security requirements have not been met."
            );
        }

        public void SendEvent()
        {
            _signalboyService?.TrySendEvent();
        }

        public void _DebugTriggerSync()
        {
            _signalboyService?.TryTriggerSync();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private class ServiceConnection : AndroidJavaProxy
        {
            private readonly SignalboyBehaviour _parent;

            internal ServiceConnection(SignalboyBehaviour parent) : base("android.content.ServiceConnection")
            {
                _parent = parent;
            }

            private void onServiceConnected(AndroidJavaObject componentName, AndroidJavaObject service)
            {
                // service: SignalboyService.LocalBinder
                var signalboyService = new SignalboyServiceWrapper(service.Call<AndroidJavaObject>("getService"));
                signalboyService.SetOnConnectionStateUpdateListener(
                    new ConnectionStateUpdateListener(connectionState =>
                        _parent._uiThreadTaskFactory.StartNew(() =>
                        {
                            _parent.ConnectionStateUpdateCallback?.Invoke(connectionState);
                        }))
                );

                _parent._signalboyService = signalboyService;
            }

            private void onServiceDisconnected(AndroidJavaObject componentName)
            {
                // This is called when the connection with the service has been
                // unexpectedly disconnected -- that is, its process crashed.
                Debug.LogError("onServiceDisconnected");
                _parent._signalboyService = null;
            }
        }

        #region MonoBehaviour - lifecycle

        private void Start()
        {
            if (Application.isEditor)
            {
                Debug.Log("Running in editor: Signalboy-Service is not available.");
                return;
            }
#if PLATFORM_ANDROID
            // AndroidJNIHelper.debug = true;

            _uiThreadTaskFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
#else
            Debug.LogError("Signalboy requires Android Platform.");
#endif
        }

        private void Update()
        {
        }

        #endregion
    }

    public delegate void ConnectionStateUpdateCallback(State connectionState);
}