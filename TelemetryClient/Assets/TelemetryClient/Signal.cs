using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TelemetryClient
{
    [Serializable]
    internal struct SignalPostBody
    {
        public DateTime receivedAt;
        public Guid appID;
        public string clientUser;
        public string sessionID;
        public string type;
        public string[] payload;
    }

    [Serializable]
    internal struct SignalPayload
    {
        public string platform;
        public string unityVersion;
        public string appVersion;
        public string isDebug;
        public string modelName;
        public string operatingSystem;
        public string operatingSystemFamily;
        public string scriptingBackend;
        public string locale;
        public string telemetryClientVersion;

        public Dictionary<string, string> additionalPayload;

        public static SignalPayload GetCommonPayload(Dictionary<string, string> additionalPayload = null)
        {
            return new SignalPayload
            {
                platform = CommonValues.Platform,
                unityVersion = CommonValues.UnityVersion,
                appVersion = CommonValues.AppVersion,
                isDebug = $"{CommonValues.IsDebug}",
                modelName = CommonValues.ModelName,
                operatingSystem = CommonValues.OperatingSystem,
                operatingSystemFamily = CommonValues.OperatingSystemFamily,
                scriptingBackend = CommonValues.ScriptingBackend,
                locale = CommonValues.Locale,
                telemetryClientVersion = TelemetryManager.TelemetryClientVersion,
                additionalPayload = additionalPayload
            };
        }
    }

    /*
    /// Converts the `additionalPayload` to a `[String: String]` dictionary
    Dictionary<string, string> ToDictionary()
        {
        /// We need to convert the additionalPayload into new key/value pairs

        // encoder.dateEncodingStrategy = .iso8601;
        try
        {
            /// Create a Dictionary
            var jsonData = JsonUtility.ToJson(this);
            var dict = try JSONSerialization.jsonObject(with: jsonData, options: []) as?[String: Any]
        /// Remove the additionalPayload sub dictionary
        dict?.removeValue(forKey: "additionalPayload")
        /// Add the additionalPayload as new key/value pairs
        return dict?.merging(additionalPayload, uniquingKeysWith: { _, last in last }) as?[String: String] ?? [:]
    }
            catch
            {
                return [:]
            }
            }

string[] ToMultiValueDimension()
            {
                return ToDictionary().map { key, value in key.replacingOccurrences(of: ":", with: "_") + ":" + value }
            }

        }
    */

    static class CommonValues
    {
        // TODO testflight/appstore equivalent?

#if DEBUG
        public static bool IsDebug => true;
#else
            public static bool IsDebug => false;
#endif

        /// The operating system and its version
        public static string OperatingSystem => SystemInfo.operatingSystem;

        public static string AppVersion => Application.version;

        public static string UnityVersion => Application.unityVersion;

        /// The modelname as reported by SystemInfo.deviceModel
        public static string ModelName => SystemInfo.deviceModel;

        public static string OperatingSystemFamily => SystemInfo.operatingSystemFamily.ToString();

        /// The operating system as reported by Swift. Note that this will report catalyst apps and iOS apps running on
        /// macOS as "iOS". See `platform` for an alternative.
        /// See https://docs.unity3d.com/Manual/PlatformDependentCompilation.html
        public static string Platform => Application.platform.ToString();

        public static string ScriptingBackend
        {
            get
            {
#if ENABLE_MONO
                return "Mono";
#elif ENABLE_IL2CPP
                return "IL2CPP";
#else
                return "Unknown";
#endif
            }
        }

        /// The locale identifier
        public static string Locale => System.Globalization.CultureInfo.CurrentCulture.Name;
    }
}