using System;
using UnityEngine;

#nullable enable

// C#-Wrappers, that adapt the Android-Java classes provided by the
// companion Signalboy helper library for Android.
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
