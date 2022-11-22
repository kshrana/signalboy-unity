#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

// C#-Wrappers, that adapt the Android-Java classes provided by the
// companion Signalboy helper library for Android.
namespace Signalboy.Wrappers
{
    public class SignalboyService
    {
        internal const string CLASSNAME = "de.kishorrana.signalboy_android.service.SignalboyService";

        private readonly AndroidJavaObject _signalboyServiceInstance;

        internal SignalboyService(AndroidJavaObject signalboyServiceInstance)
        {
            _signalboyServiceInstance = signalboyServiceInstance;
        }

        internal static string EXTRA_CONFIGURATION
        {
            get
            {
                using var jc = new AndroidJavaClass(CLASSNAME);
                return jc.GetStatic<string>("EXTRA_CONFIGURATION");
            }
        }

        public State State =>
            StateFactory.Wrapping(_signalboyServiceInstance.Get<AndroidJavaObject>("state"));

        public bool HasUserInteractionRequest =>
            _signalboyServiceInstance.Call<bool>("getHasUserInteractionRequest");

        internal static AndroidJavaObject GetDefaultAdapter(AndroidJavaObject context)
        {
            using var jc = new AndroidJavaClass(CLASSNAME);
            return jc.CallStatic<AndroidJavaObject>("getDefaultAdapter", context);
        }

        internal static PrerequisitesResult VerifyPrerequisites(
            AndroidJavaObject context,
            AndroidJavaObject bluetoothAdapter
        )
        {
            using var jc = new AndroidJavaClass(CLASSNAME);
            var resultJavaObject = jc.CallStatic<AndroidJavaObject>(
                "verifyPrerequisites",
                context,
                bluetoothAdapter
            );

            return new PrerequisitesResult(resultJavaObject);
        }

        private static AndroidJavaObject /*<AssociateFragment>*/
            InjectAssociateFragment(AndroidJavaObject fragmentManager)
        {
            using var jc = new AndroidJavaClass(CLASSNAME);
            return jc.CallStatic<AndroidJavaObject>("injectAssociateFragment", fragmentManager);
        }

        internal static async Task<AndroidJavaObject> /*<AssociateFragment>*/
            InjectAssociateFragmentAsync(AndroidJavaObject activity)
        {
            var tcs = MakeTaskCompletionSource<AndroidJavaObject>();
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                using var fragmentManager = activity.Call<AndroidJavaObject>("getFragmentManager");

                AndroidJavaObject fragment;
                try
                {
                    using var jc = new AndroidJavaClass(CLASSNAME);
                    fragment = jc.CallStatic<AndroidJavaObject>("injectAssociateFragment", fragmentManager);
                }
                catch (Exception exception)
                {
                    tcs.TrySetException(exception);
                    return;
                }

                tcs.TrySetResult(fragment);
            }));

            return await tcs.Task;
        }

        // May throw.
        internal async Task ResolveUserInteractionRequest(
            AndroidJavaObject activity,
            AndroidJavaObject userInteractionProxy
        )
        {
            var tcs = MakeTaskCompletionSource<object>();

            using (var helperClass =
                   new AndroidJavaClass("de.kishorrana.signalboy_android.service.SignalboyServiceJavaInterop"))
            {
                helperClass.CallStatic(
                    "resolveUserInteractionRequest",
                    _signalboyServiceInstance,
                    activity,
                    userInteractionProxy,
                    new InteractionRequestCallback(
                        () => tcs.TrySetResult(null!),
                        throwable =>
                        {
                            string javaString;
                            try
                            {
                                javaString = throwable.Call<string>("toString");
                            }
                            catch
                            {
                                javaString = "";
                                Debug.LogError("Failed to get debug-string from java-object `toString()`.");
                            }

                            Exception exception = new InvalidOperationException(
                                "Failed to resolve Interaction Request, due to exception (in Java-code): " +
                                javaString
                            );
                            tcs.TrySetException(exception);
                        }
                    )
                );
            }

            await tcs.Task;
        }

        internal void TrySendEvent()
        {
            _signalboyServiceInstance.Call("trySendEvent");
        }

        internal void TryTriggerSync()
        {
            _signalboyServiceInstance.Call<bool>("tryTriggerSync");
        }

        internal void SetOnConnectionStateUpdateListener(ConnectionStateUpdateListener callback)
        {
            _signalboyServiceInstance.Call("setOnConnectionStateUpdateListener", callback);
        }

        internal void UnsetOnConnectionStateUpdateListener()
        {
            _signalboyServiceInstance.Call("unsetOnConnectionStateUpdateListener");
        }

        private static TaskCompletionSource<T> MakeTaskCompletionSource<T>()
        {
            return new TaskCompletionSource<T>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
        }

        public class Configuration
        {
            internal const string CLASSNAME = "de.kishorrana.signalboy_android.service.SignalboyService$Configuration";

            private static readonly Lazy<Configuration> _Default = new(() =>
                {
                    using var jc = new AndroidJavaClass(CLASSNAME);
                    return new Configuration(jc.CallStatic<AndroidJavaObject>("getDefault"));
                }
            );

            private readonly AndroidJavaObject _javaInstance;

            public Configuration(long normalizationDelay) : this(null)
            {
                NormalizationDelay = normalizationDelay;
            }

            private Configuration(AndroidJavaObject? javaInstance)
            {
                if (javaInstance != null)
                    _javaInstance = javaInstance;
                else
                    _javaInstance = new AndroidJavaObject(CLASSNAME);
            }

            public static Configuration Default => _Default.Value;

            public long NormalizationDelay
            {
                get => _javaInstance.Get<long>("normalizationDelay");
                set => _javaInstance.Set("normalizationDelay", value);
            }

            internal AndroidJavaObject GetJavaInstance() => _javaInstance;
        }

        [DebuggerDisplay("UnmetPrerequisite={UnmetPrerequisites}")]
        public class PrerequisitesResult
        {
            private readonly AndroidJavaObject _javaInstance;

            internal PrerequisitesResult(AndroidJavaObject javaInstance)
            {
                _javaInstance = javaInstance;
            }

            public List<Prerequisite> UnmetPrerequisites
            {
                get
                {
                    using var javaList = _javaInstance.Get<AndroidJavaObject>("unmetPrerequisites");
                    var javaObjects = AndroidJNICollectionHelper
                        .ConvertFromAndroidJavaList<AndroidJavaObject>(javaList);

                    return javaObjects.ConvertAll(PrerequisiteFactory.Wrapping);
                }
            }
        }

        public abstract class Prerequisite
        {
            private readonly AndroidJavaObject _javaInstance;

            private Prerequisite(AndroidJavaObject javaInstance)
            {
                _javaInstance = javaInstance;
            }

            public class BluetoothEnabledPrerequisite : Prerequisite
            {
                internal const string CLASSNAME =
                    "de.kishorrana.signalboy_android.service.SignalboyPrerequisitesHelper$Prerequisite$BluetoothEnabledPrerequisite";

                internal BluetoothEnabledPrerequisite(AndroidJavaObject javaInstance) : base(javaInstance)
                {
                }
            }

            [DebuggerDisplay("RuntimePermissions={RuntimePermissions}")]
            public class RuntimePermissionsPrerequisite : Prerequisite
            {
                internal const string CLASSNAME =
                    "de.kishorrana.signalboy_android.service.SignalboyPrerequisitesHelper$Prerequisite$RuntimePermissionsPrerequisite";

                internal RuntimePermissionsPrerequisite(AndroidJavaObject javaInstance) : base(javaInstance)
                {
                }

                public List<string> RuntimePermissions => AndroidJNICollectionHelper.ConvertFromAndroidJavaList<string>(
                    _javaInstance.Get<AndroidJavaObject>("runtimePermissions")
                );
            }

            [DebuggerDisplay("UsesFeatures={UsesFeatures}")]
            public class UsesFeatureDeclarationsPrerequisite : Prerequisite
            {
                internal const string CLASSNAME =
                    "de.kishorrana.signalboy_android.service.SignalboyPrerequisitesHelper$Prerequisite$UsesFeatureDeclarationsPrerequisite";

                internal UsesFeatureDeclarationsPrerequisite(AndroidJavaObject javaInstance) : base(javaInstance)
                {
                }

                public List<string> UsesFeatures => AndroidJNICollectionHelper.ConvertFromAndroidJavaList<string>(
                    _javaInstance.Get<AndroidJavaObject>("usesFeatures")
                );
            }
        }

        private static class PrerequisiteFactory
        {
            internal static Prerequisite Wrapping(AndroidJavaObject javaObject)
            {
                // Note: Nested classes are denoted with "$"-token
                // e.g. "Lde.kishorrana.signalboy_android.service.SignalboyPrerequisitesHelper$Prerequisite$BluetoothEnabledPrerequisite"
                var javaSignature = AndroidJNIHelper.GetSignature(javaObject);
                if (javaSignature == null) throw new ArgumentNullException(nameof(javaSignature));

                return javaSignature switch
                {
                    string when javaSignature.Contains(Prerequisite.BluetoothEnabledPrerequisite.CLASSNAME) =>
                        new Prerequisite.BluetoothEnabledPrerequisite(javaObject),
                    string when javaSignature.Contains(Prerequisite.RuntimePermissionsPrerequisite.CLASSNAME) =>
                        new Prerequisite.RuntimePermissionsPrerequisite(javaObject),
                    string when javaSignature.Contains(Prerequisite.UsesFeatureDeclarationsPrerequisite.CLASSNAME) =>
                        new Prerequisite.UsesFeatureDeclarationsPrerequisite(javaObject),
                    _ => throw new ArgumentException($"Unknown case: {javaSignature}")
                };
            }
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    internal class ConnectionStateUpdateListener : AndroidJavaProxy
    {
        private readonly ConnectionStateUpdateCallback _connectionStateUpdateCallback;

        internal ConnectionStateUpdateListener(ConnectionStateUpdateCallback callback) : base(
            "de.kishorrana.signalboy_android.service.SignalboyService$OnConnectionStateUpdateListener"
        )
        {
            _connectionStateUpdateCallback = callback;
        }

        private void stateUpdated(AndroidJavaObject state)
        {
            _connectionStateUpdateCallback(StateFactory.Wrapping(state));
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    internal class InteractionRequestCallback : AndroidJavaProxy
    {
        public delegate void ErrorCallback(AndroidJavaObject throwable);

        public delegate void VoidCallback();

        private readonly ErrorCallback _failureCallback;

        private readonly VoidCallback _successCallback;

        internal InteractionRequestCallback(VoidCallback successCallback, ErrorCallback failureCallback) : base(
            "de.kishorrana.signalboy_android.service.InteractionRequestCallback")
        {
            _successCallback = successCallback;
            _failureCallback = failureCallback;
        }

        private void requestFinishedSuccessfully()
        {
            _successCallback();
        }

        private void requestFinishedExceptionally(AndroidJavaObject throwable)
        {
            _failureCallback(throwable);
        }
    }

    public class SignalboyDeviceInformation
    {
        internal SignalboyDeviceInformation(AndroidJavaObject javaObject)
        {
            LocalName = javaObject.Get<string>("localName");
            HardwareRevision = javaObject.Get<string>("hardwareRevision");
            SoftwareRevision = javaObject.Get<string>("softwareRevision");
        }

        public string LocalName { get; }
        public string HardwareRevision { get; }
        public string SoftwareRevision { get; }

        public void Deconstruct(
            out string localName,
            out string hardwareRevision,
            out string softwareRevision
        )
        {
            localName = LocalName;
            hardwareRevision = HardwareRevision;
            softwareRevision = SoftwareRevision;
        }
    }

    public abstract class State
    {
        private readonly AndroidJavaObject _javaObject;

        private State(AndroidJavaObject javaObject)
        {
            _javaObject = javaObject;
        }

        public sealed class StateDisconnected : State
        {
            internal StateDisconnected(AndroidJavaObject javaObject) : base(javaObject)
            {
            }

            public AndroidJavaObject? Cause => _javaObject.Get<AndroidJavaObject>("cause");
        }

        public sealed class StateConnecting : State
        {
            internal StateConnecting(AndroidJavaObject javaObject) : base(javaObject)
            {
            }
        }

        public sealed class StateConnected : State
        {
            internal StateConnected(AndroidJavaObject javaObject) : base(javaObject)
            {
            }

            public SignalboyDeviceInformation DeviceInformation =>
                new(_javaObject.Get<AndroidJavaObject>("deviceInformation"));

            public bool IsSynced => _javaObject.Get<bool>("isSynced");
        }
    }

    internal static class StateFactory
    {
        internal static State Wrapping(AndroidJavaObject javaObject)
        {
            // Note: Nested classes are denoted with "$"-token
            // e.g. "Lde.kishorrana.signalboy_android.service.SignalboyService$State$Connected"
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
