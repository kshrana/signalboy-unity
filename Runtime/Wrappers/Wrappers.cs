using System;
using System.Diagnostics;
using UnityEngine;

#nullable enable

// C#-Wrappers, that adapt the Android-Java classes provided by the
// companion Signalboy helper library for Android.
namespace Signalboy.Wrappers
{
    public class SignalboyFacadeWrapper
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

        public State State => StateFactory.Wrapping(signalboyServiceInstance.Get<AndroidJavaObject>("state"));

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

        internal static PrerequisitesResult VerifyPrerequisites(AndroidJavaObject context, AndroidJavaObject bluetoothAdapter)
        {
            using (AndroidJavaClass jc = new AndroidJavaClass(CLASSNAME))
            {
                var javaObject = jc.CallStatic<AndroidJavaObject>("verifyPrerequisites", context, bluetoothAdapter);
                return new PrerequisitesResult(javaObject);
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

        public class Configuration
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

            public static Configuration Default => _Default.Value;

            public long NormalizationDelay
            {
                get => javaInstance.Get<long>("normalizationDelay");
                set => javaInstance.Set<long>("normalizationDelay", value);
            }

            private AndroidJavaObject javaInstance;

            public Configuration(long normalizationDelay) : this(null)
            {
                this.NormalizationDelay = normalizationDelay;
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

        [DebuggerDisplay("UnmetPrerequisite={UnmetPrerequisite}")]
        public class PrerequisitesResult
        {
            internal const string CLASSNAME = "de.kishorrana.signalboy_android.SignalboyFacade$PrerequisitesResult";

            public Prerequisite? UnmetPrerequisite
            {
                get
                {
                    var javaObject = javaInstance.Get<AndroidJavaObject>("unmetPrerequisite");
                    return javaObject != null ? PrerequisiteFactory.Wrapping(javaObject) : null;
                }
            }

            private AndroidJavaObject javaInstance;

            internal PrerequisitesResult(AndroidJavaObject javaInstance)
            {
                this.javaInstance = javaInstance;
            }
        }

        public abstract class Prerequisite
        {
            internal const string CLASSNAME = "de.kishorrana.signalboy_android.SignalboyFacade$Prerequisite";

            private protected AndroidJavaObject javaInstance;

            internal Prerequisite(AndroidJavaObject javaInstance)
            {
                this.javaInstance = javaInstance;
            }

            public class BluetoothEnabledPrerequisite : Prerequisite
            {
                internal BluetoothEnabledPrerequisite(AndroidJavaObject javaInstance) : base(javaInstance) { }
            }

            [DebuggerDisplay("permission={permission}")]
            public class RuntimePermissionsPrerequisite : Prerequisite
            {
                public string permission => javaInstance.Get<string>("permission");
                internal RuntimePermissionsPrerequisite(AndroidJavaObject javaInstance) : base(javaInstance) { }
            }
        }

        internal static class PrerequisiteFactory
        {
            internal static Prerequisite Wrapping(AndroidJavaObject javaObject)
            {
                // Note: Nested classes are denoted with "$"-token
                // e.g. "Lde.kishorrana.signalboy_android.SignalboyFacade$Prerequisite$BluetoothEnabledPrerequisite"
                var javaSignature = AndroidJNIHelper.GetSignature(javaObject);
                switch (javaSignature)
                {
                    case string it when it.Contains("$Prerequisite$BluetoothEnabledPrerequisite"):
                        return new Prerequisite.BluetoothEnabledPrerequisite(javaObject);
                    case string it when it.Contains("$Prerequisite$RuntimePermissionsPrerequisite"):
                        return new Prerequisite.RuntimePermissionsPrerequisite(javaObject);
                    case null:
                        throw new ArgumentNullException(nameof(javaSignature));
                    default:
                        throw new ArgumentException($"Unknown case: {javaSignature}");
                }
            }
        }
    }

    internal class ConnectionStateUpdateListener : AndroidJavaProxy
    {
        internal ConnectionStateUpdateCallback ConnectionStateUpdateCallback;

        internal ConnectionStateUpdateListener(ConnectionStateUpdateCallback callback) : base(
            "de.kishorrana.signalboy_android.SignalboyFacade$OnConnectionStateUpdateListener"
        )
        {
            this.ConnectionStateUpdateCallback = callback;
        }

        private void stateUpdated(AndroidJavaObject state)
        {
            ConnectionStateUpdateCallback(StateFactory.Wrapping(state));
        }
    }

    public class SignalboyDeviceInformation
    {
        private string _hardwareRevision;
        public string HardwareRevision => _hardwareRevision;

        private string _softwareRevision;
        public string SoftwareRevision => _softwareRevision;

        internal SignalboyDeviceInformation(AndroidJavaObject javaObject)
        {
            _hardwareRevision = javaObject.Get<string>("hardwareRevision");
            _softwareRevision = javaObject.Get<string>("softwareRevision");
        }

        public void Deconstruct(out string hRevision, out string sRevision)
        {
            hRevision = HardwareRevision;
            sRevision = SoftwareRevision;
        }
    }

    public abstract class State
    {
        private protected AndroidJavaObject javaObject;

        public State(AndroidJavaObject javaObject)
        {
            this.javaObject = javaObject;
        }

        public sealed class StateDisconnected : State
        {
            public AndroidJavaObject? Cause => javaObject.Get<AndroidJavaObject>("cause");

            internal StateDisconnected(AndroidJavaObject javaObject) : base(javaObject) { }
        }

        public sealed class StateConnecting : State
        {
            internal StateConnecting(AndroidJavaObject javaObject) : base(javaObject) { }
        }

        public sealed class StateConnected : State
        {
            public SignalboyDeviceInformation DeviceInformation => new SignalboyDeviceInformation(javaObject.Get<AndroidJavaObject>("deviceInformation"));
            public bool IsSynced => javaObject.Get<bool>("isSynced");

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
