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

        public void VerifyPrerequisites()
        {
            using (
                var context = Helper.GetCurrentActivity()
            )
            {
                var bluetoothAdapter = SignalboyFacadeWrapper.GetDefaultAdapter(context);
                SignalboyFacadeWrapper.VerifyPrerequisites(context, bluetoothAdapter);
            }
        }

        public void BindService()
        {
            using (
                var context = Helper.GetCurrentActivity()
            )
            {
                var intent = new AndroidJavaObject("android.content.Intent");
                using (var componentName = new AndroidJavaObject("android.content.ComponentName", context, SignalboyFacadeWrapper.CLASSNAME))
                {
                    intent = intent.Call<AndroidJavaObject>("setComponent", componentName);
                }
                var configuration = SignalboyFacadeWrapper.Configuration.Default;
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
}

namespace Signalboy
{
    public delegate void ConnectionStateUpdateCallback(State connectionState);

    internal class Helper
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

namespace Signalboy.Wrappers
{
    internal class SignalboyFacadeWrapper
    {
        internal const string CLASSNAME = "de.kishorrana.signalboy_android.SignalboyFacade";
        internal static string EXTRA_CONFIGURATION
        {
            get
            {
                using (AndroidJavaClass jc = new AndroidJavaClass(CLASSNAME))
                {
                    return jc.GetStatic<string>("EXTRA_CONFIGURATION");
                }
            }
        }

        public State state => StateFactory.Wrapping(signalboyServiceInstance.Get<AndroidJavaObject>("state"));

        private AndroidJavaObject signalboyServiceInstance;

        internal SignalboyFacadeWrapper(AndroidJavaObject signalboyServiceInstance)
        {
            this.signalboyServiceInstance = signalboyServiceInstance;
        }

        internal static AndroidJavaObject GetDefaultAdapter(AndroidJavaObject context)
        {
            AndroidJavaObject bluetoothAdapter;
            using (AndroidJavaClass jc = new AndroidJavaClass(CLASSNAME))
            {
                bluetoothAdapter = jc.CallStatic<AndroidJavaObject>("getDefaultAdapter", context);
            }

            return bluetoothAdapter;
        }

        internal static void VerifyPrerequisites(AndroidJavaObject context, AndroidJavaObject bluetoothAdapter)
        {
            using (AndroidJavaClass jc = new AndroidJavaClass(CLASSNAME))
            {
                jc.CallStatic("verifyPrerequisites", context, bluetoothAdapter);
            }
        }

        internal void SendEvent()
        {
            signalboyServiceInstance.Call("sendEvent");
        }

        internal void TryTriggerSync()
        {
            signalboyServiceInstance.Call<bool>("tryTriggerSync");
        }

        internal void SetOnConnectionStateUpdateListener(ConnectionStateUpdateListener callback)
        {
            signalboyServiceInstance.Call("setOnConnectionStateUpdateListener", callback);
        }

        internal void UnsetOnConnectionStateUpdateListener()
        {
            signalboyServiceInstance.Call("unsetOnConnectionStateUpdateListener");
        }

        internal class Configuration
        {
            internal const string CLASSNAME = "de.kishorrana.signalboy_android.SignalboyFacade$Configuration";

            private static Lazy<Configuration> _Default = new Lazy<Configuration>(() =>
            {
                using (AndroidJavaClass jc = new AndroidJavaClass(CLASSNAME))
                {
                    return new Configuration(jc.CallStatic<AndroidJavaObject>("getDefault"));
                }
            }
            );

            internal static Configuration Default => _Default.Value;

            internal long normalizationDelay
            {
                get => javaInstance.Get<long>("normalizationDelay");
                set => javaInstance.Set<long>("normalizationDelay", value);
            }

            private AndroidJavaObject javaInstance;

            internal Configuration(long normalizationDelay) : this(null)
            {
                this.normalizationDelay = normalizationDelay;
            }

            private Configuration(AndroidJavaObject? javaInstance)
            {
                if (javaInstance != null)
                {
                    this.javaInstance = javaInstance;
                }
                else
                {
                    using (AndroidJavaClass jc = new AndroidJavaClass(CLASSNAME))
                    {
                        this.javaInstance = new AndroidJavaObject(CLASSNAME);
                    }
                }
            }

            internal AndroidJavaObject GetJavaInstance() => javaInstance;
        }
    }

    internal class ConnectionStateUpdateListener : AndroidJavaProxy
    {
        internal ConnectionStateUpdateCallback connectionStateUpdateCallback;

        internal ConnectionStateUpdateListener(ConnectionStateUpdateCallback callback) : base(
            "de.kishorrana.signalboy_android.SignalboyFacade$OnConnectionStateUpdateListener"
        )
        {
            this.connectionStateUpdateCallback = callback;
        }

        private void stateUpdated(AndroidJavaObject state)
        {
            connectionStateUpdateCallback(StateFactory.Wrapping(state));
        }
    }

    public class SignalboyDeviceInformation
    {
        public string hardwareRevision;
        public string softwareRevision;

        internal SignalboyDeviceInformation(AndroidJavaObject javaObject)
        {
            hardwareRevision = javaObject.Get<string>("hardwareRevision");
            softwareRevision = javaObject.Get<string>("softwareRevision");
        }

        public void Deconstruct(out string hRevision, out string sRevision)
        {
            hRevision = hardwareRevision;
            sRevision = softwareRevision;
        }
    }

    public abstract class State
    {
        protected AndroidJavaObject javaObject;

        public State(AndroidJavaObject javaObject)
        {
            this.javaObject = javaObject;
        }

        public sealed class StateDisconnected : State
        {
            public AndroidJavaObject? cause => javaObject.Get<AndroidJavaObject>("cause");

            internal StateDisconnected(AndroidJavaObject javaObject) : base(javaObject) { }
        }

        public sealed class StateConnecting : State
        {
            internal StateConnecting(AndroidJavaObject javaObject) : base(javaObject) { }
        }

        public sealed class StateConnected : State
        {
            public SignalboyDeviceInformation deviceInformation => new SignalboyDeviceInformation(javaObject.Get<AndroidJavaObject>("deviceInformation"));
            public bool isSynced => javaObject.Get<bool>("isSynced");

            internal StateConnected(AndroidJavaObject javaObject) : base(javaObject) { }
        }
    }

    internal static class StateFactory
    {
        internal static State Wrapping(AndroidJavaObject javaObject)
        {
            // Note: Nested classes are denoted with "$"-token
            // e.g. "Lde.kishorrana.signalboy_android.SignalboyFacade$State$Connected"
            var javaSignature = AndroidJNIHelper.GetSignature(javaObject);
            switch (javaSignature)
            {
                case string it when it.Contains("$State$Disconnected"):
                    return new State.StateDisconnected(javaObject);
                case string it when it.Contains("$State$Connecting"):
                    return new State.StateConnecting(javaObject);
                case string it when it.Contains("$State$Connected"):
                    return new State.StateConnected(javaObject);
                case null:
                    throw new ArgumentNullException(nameof(javaSignature));
                default:
                    throw new ArgumentException($"Unknown case: {javaSignature}");
            }
        }
    }
}
