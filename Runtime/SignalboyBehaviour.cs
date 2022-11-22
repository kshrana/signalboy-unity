#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Signalboy.Wrappers;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Signalboy
{
    public class SignalboyBehaviour : MonoBehaviour
    {
        // Not-null when Android-Service has been bound successfully.
        private SignalboyService? _signalboyService;
        public ConnectionStateUpdateCallback? ConnectionStateUpdateCallback;
        public State? State => _signalboyService?.State;
        public bool HasUserInteractionRequest => _signalboyService?.HasUserInteractionRequest ?? false;

        private TaskFactory _uiThreadTaskFactory = null!;

        public SignalboyService.PrerequisitesResult VerifyPrerequisites()
        {
            var context = AndroidHelper.GetCurrentActivity();
            var bluetoothAdapter = SignalboyService.GetDefaultAdapter(context);
            return SignalboyService.VerifyPrerequisites(context, bluetoothAdapter);
        }

        // convenience method
        public void BindService()
        {
            BindService(SignalboyService.Configuration.Default);
        }

        public void BindService(SignalboyService.Configuration configuration)
        {
            using var context = AndroidHelper.GetCurrentActivity().Call<AndroidJavaObject>("getApplicationContext");
            var intent = new AndroidJavaObject("android.content.Intent");
            using (var componentName = new AndroidJavaObject("android.content.ComponentName", context,
                       SignalboyService.CLASSNAME))
            {
                intent = intent.Call<AndroidJavaObject>("setComponent", componentName);
            }

            intent = intent.Call<AndroidJavaObject>("putExtra", SignalboyService.EXTRA_CONFIGURATION,
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

        public async Task ResolveUserInteractionRequest()
        {
            using var activity = AndroidHelper.GetCurrentActivity();
            if (activity == null)
            {
                throw new InvalidOperationException();
            }

            using var userInteractionProxy = await SignalboyService.InjectAssociateFragmentAsync(activity);
            if (_signalboyService != null)
            {
                await _signalboyService.ResolveUserInteractionRequest(activity, userInteractionProxy);
            }
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
                var signalboyService = new SignalboyService(service.Call<AndroidJavaObject>("getService"));
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
}
