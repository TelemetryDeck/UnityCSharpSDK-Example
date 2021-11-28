using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TelemetryClient
{
    using TelemetrySignalType = String;

    internal class SignalManager : MonoBehaviour
    {
        private const float MINIMUM_WAIT_TIME_BETWEEN_REQUESTS = 10; // seconds

        private SignalCache<SignalPostBody> signalCache;
        private readonly TelemetryManagerConfiguration configuration;
        private Coroutine sendCoroutine = null;

        public SignalManager(TelemetryManagerConfiguration configuration)
        {
            this.configuration = configuration;

            // We automatically load any old signals from disk on initialisation
            signalCache = new SignalCache<SignalPostBody>(showDebugLogs: configuration.showDebugLogs);

            StartTimer();
        }

        /// <summary>
        /// Setup a timer to send the Signals
        /// </summary>
        private void StartTimer()
        {
            if (sendCoroutine != null)
            {
                StopCoroutine(sendCoroutine);
                sendCoroutine = null;
            }
            IEnumerator SendSignals()
            {
                while (true)
                {
                    // Fire the signal immediately to attempt to send any cached Signals from a previous session
                    CheckForSignalsAndSend();
                    yield return new WaitForSeconds(MINIMUM_WAIT_TIME_BETWEEN_REQUESTS);
                }
            }
            sendCoroutine = StartCoroutine(SendSignals());
        }

        /// <summary>
        /// Adds a signal to the process queue
        /// </summary>
        internal void ProcessSignal(TelemetryManagerConfiguration configuration, TelemetrySignalType signalType, string clientUser = null, Dictionary<string, string> additionalPayload = null)
        {
            //DispatchQueue.global(qos: .utility).async {
            //    [self] in

            var payLoad = new SignalPayload()
            {
                additionalPayload = additionalPayload
            };

            var userHash = ComputeSha256Hash(clientUser ?? DefaultUserIdentifier, Encoding.Unicode);

            var signalPostBody = new SignalPostBody()
            {
                receivedAt = DateTime.Now,
                appID = new Guid(configuration.TelemetryAppID),
                clientUser = userHash,
                sessionID = configuration.SessionId.ToString(),
                type = $"{signalType}",
                payload = payLoad.ToMultiValueDimension()
            };

            if (configuration.showDebugLogs)
                Debug.Log($"Process signal: {signalPostBody}");

            signalCache.Push(signalPostBody);
            //}
        }

        /// <summary>
        /// Send signals once we have more than the minimum.
        /// If any fail to send, we put them back into the cache to send later.
        /// </summary>
        private void CheckForSignalsAndSend()
        {
            if (configuration.showDebugLogs)
            {
                Debug.Log($"Current signal cache count: {signalCache.Count}");
            }

            var queuedSignals = signalCache.Pop();
            if (queuedSignals.Count > 0)
            {
                if (configuration.showDebugLogs)
                {
                    Debug.Log($"Sending {queuedSignals.Count} signals leaving a cache of {signalCache.Count} signals");
                }

                Send(queuedSignals, completion: (data, response, error) =>
                {
                    if (error != null)
                    {
                        if (configuration.showDebugLogs)
                        {
                            Debug.LogError(error);
                        }
                    // The send failed, put the signal back into the queue

                    signalCache.Push(queuedSignals);
                        return;
                    }

                // Check for valid status code response
                if (!string.IsNullOrEmpty(error))
                    {
                        if (configuration.showDebugLogs)
                        {
                            Debug.LogError(error);
                        }
                    // The send failed, put the signal back into the queue
                    signalCache.Push(queuedSignals);
                        return;
                    }
                    else if (data != null)
                    {
                        if (configuration.showDebugLogs)
                        {
                            Debug.Log(data);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Before the app terminates, we want to save any pending signals to disk
        /// </summary>
        private void OnDestroy()
        {
            if (configuration.showDebugLogs)
            {
                Debug.Log("App will terminate");
            }

            signalCache.BackupCache();
        }

        private void Send(List<SignalPostBody> signalPostBodies, Action<string, int, string> completion)
        {
            var stringBuilder = new StringBuilder(capacity: 90);
            stringBuilder.Append(configuration.ApiBaseUrl);
            stringBuilder.Append("/api/v1/apps/");
            stringBuilder.Append(configuration.TelemetryAppID);
            stringBuilder.Append("/signals/multiple/");
            string url = stringBuilder.ToString();

            string data = JsonUtility.ToJson(signalPostBodies);
            if (configuration.showDebugLogs)
                Debug.Log(data);
            var webRequest = UnityWebRequest.Post(url, data);

            webRequest.SetRequestHeader("Content-Type", "application/json");

            UnityWebRequestAsyncOperation asyncOperation = webRequest.SendWebRequest();
            asyncOperation.completed += (operation) =>
            {
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        completion(null, -1, "Network error");
                        break;
                    // TODO
                }
            };
        }

        #region Helpers
        /// <summary>
        /// The default user identifier. If the platform supports it, the identifierForVendor. Otherwise, system version
        /// and build number (in which case it's strongly recommended to supply an email or UUID or similar identifier for
        /// your user yourself.
        /// </summary>
        public string DefaultUserIdentifier
        {
            get
            {
                if (configuration.defaultUser != null)
                {
                    return configuration.defaultUser;
                }
                else
                {
                    return "TODO";// UIDevice.current.identifierForVendor?.uuidString ?? "unknown user \(SignalPayload.systemVersion) \(SignalPayload.buildNumber)";
                }
                // TODO get uuid
                // else the following
                /*
            #if DEBUG
                Debug.LogWarning("[Telemetry] On this platform, Telemetry can't generate a unique user identifier. It is recommended you supply one yourself. More info: https://telemetrydeck.com/pages/signal-reference.html")
            #else
            return "unknown user \(SignalPayload.platform) \(SignalPayload.systemVersion) \(SignalPayload.buildNumber)"
            #endif
                */
            }
        }

        /// <summary>
        /// Computes SHA256 Hash using .NET Cryptography library.
        /// </summary>
        /// <param name="rawData">Input data to be hashed.</param>
        /// <returns></returns>
        static string ComputeSha256Hash(string rawData, Encoding encoding)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(encoding.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        #endregion

        private struct TelemetryServerError
        {
            public enum EKind
            {
                Unknown, Unauthorised, Forbidden, PayloadTooLarge, InvalidStatusCode
            }

            public EKind kind;
            public int? statusCode;

            public override string ToString()
            {
                switch (kind)
                {
                    case EKind.InvalidStatusCode:
                        return $"Invalid status code {statusCode ?? -1}";
                    case EKind.Unauthorised:
                        return "Unauthorized (401)";
                    case EKind.Forbidden:
                        return "Forbidden (403)";
                    case EKind.PayloadTooLarge:
                        return "Payload is too large (413)";
                    default:
                        return "Unknown Error";
                }
            }
        }
    }
}
