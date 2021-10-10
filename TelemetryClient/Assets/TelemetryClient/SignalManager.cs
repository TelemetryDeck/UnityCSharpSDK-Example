using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TelemetryClient
{
    internal class SignalManager: MonoBehaviour
    {
        private const float MINIMUM_WAIT_TIME_BETWEEN_REQUESTS = 10; // seconds
    
        private SignalCache<SignalPostBody> signalCache;
        private readonly TelemetryManagerConfiguration configuration;
        private Coroutine sendCoroutine = null;

        public SignalManager(TelemetryManagerConfiguration configuration)
        {
            this.configuration = configuration;

            // We automatically load any old signals from disk on initialisation
            signalCache = new SignalCache(showDebugLogs: configuration.showDebugLogs);

            StartTimer();
        }

        /// Setup a timer to send the Signals
        private void StartTimer()
        {
            if (sendCoroutine != null)
            {
                StopCoroutine(sendCoroutine);
                sendCoroutine = null;
            }
            //Timer.scheduledTimer(timeInterval: minimumWaitTimeBetweenRequests, target: self, selector: #selector(checkForSignalsAndSend), userInfo: nil, repeats: true)
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

        /// Adds a signal to the process queue
        private void processSignal(TelemetryManagerConfiguration configuration, TelemetrySignalType signalType, string clientUser = null, Dictionary<string, string> additionalPayload = null)
        {
            /*
            DispatchQueue.global(qos: .utility).async {
                [self] in

            let payLoad = SignalPayload(additionalPayload: additionalPayload)
    

            let signalPostBody = SignalPostBody(
                receivedAt: Date(),
                appID: UUID(uuidString: configuration.telemetryAppID)!,
                clientUser: sha256(str: clientUser ?? defaultUserIdentifier),
                sessionID: configuration.sessionID.uuidString,
                type: "\(signalType)",
                payload: payLoad.toMultiValueDimension())
    

            if configuration.showDebugLogs {
                    print("Process signal: \(signalPostBody)")
            }

                signalCache.push(signalPostBody)
            }
            */
        }

        /// Send signals once we have more than the minimum.
        /// If any fail to send, we put them back into the cache to send later.
        private void CheckForSignalsAndSend()
        {
            if (configuration.showDebugLogs)
            {
                Debug.Log($"Current signal cache count: {signalCache.Count}");
            }

            var queuedSignals = signalCache.Pop();
            if (!queuedSignals.isEmpty)
            {
                if (configuration.showDebugLogs)
                {
                    Debug.Log($"Sending {queuedSignals.count} signals leaving a cache of {signalCache.Count} signals");
                }

                send(queuedSignals, completion: (data, response, error) =>
                {
                    if (error != null) {
                        if (configuration.showDebugLogs)
                        {
                            Debug.LogError(error);
                        }
                        // The send failed, put the signal back into the queue
                        
                        signalCache.push(queuedSignals);
                        return;
                    }

                    // Check for valid status code response
                    if (response.statusCodeError != nil)
                    {
                        var statusError = response.statusCodeError();
                        if (configuration.showDebugLogs)
                        {
                            Debug.LogError(statusError);
                        }
                    }
                    // The send failed, put the signal back into the queue
                    signalCache.push(queuedSignals);
                    return;
                    }

                    if (data != null)
                    {
                    if (configuration.showDebugLogs)
                    {
                        Debug.Log(String(data: data, encoding: .utf8)!);
                    }
                }
            }
        }


        /// Before the app terminates, we want to save any pending signals to disk
        private void OnDestroy()
        {
            if (configuration.showDebugLogs)
            {
                Debug.Log("App will terminate");
            }

            signalCache.backupCache();
        }

    // MARK: - Comms
    private void send(signalPostBodies: [SignalPostBody], completion: (Data?, URLResponse?, Error?) => ())
    {
            /*
        DispatchQueue.global(qos: .utility).async {
            [self] in
            let path = "/api/v1/apps/\(configuration.telemetryAppID)/signals/multiple/"
            let url = configuration.apiBaseURL.appendingPathComponent(path)

            var urlRequest = URLRequest(url: url)
            urlRequest.httpMethod = "POST"
            urlRequest.addValue("application/json", forHTTPHeaderField: "Content-Type")

            urlRequest.httpBody = try!JSONEncoder.telemetryEncoder.encode(signalPostBodies)
            if configuration.showDebugLogs {
                print(String(data: urlRequest.httpBody!, encoding: .utf8)!)
            }

            let task = URLSession.shared.dataTask(with: urlRequest, completionHandler: completionHandler)
            task.resume()
        }
            */
    }

    // MARK: - Helpers

        /// The default user identifier. If the platform supports it, the identifierForVendor. Otherwise, system version
        /// and build number (in which case it's strongly recommended to supply an email or UUID or similar identifier for
        /// your user yourself.
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

        /**
         * Example SHA 256 Hash using CommonCrypto
         * CC_SHA256 API exposed from from CommonCrypto-60118.50.1:
         * https://opensource.apple.com/source/CommonCrypto/CommonCrypto-60118.50.1/include/CommonDigest.h.auto.html
         **/
        /*
        func sha256(str: String) -> String
        {
            if let strData = str.data(using: String.Encoding.utf8) {
                /// #define CC_SHA256_DIGEST_LENGTH     32
                /// Creates an array of unsigned 8 bit integers that contains 32 zeros
                var digest = [UInt8](repeating: 0, count: Int(CC_SHA256_DIGEST_LENGTH))
    
            /// CC_SHA256 performs digest calculation and places the result in the caller-supplied buffer for digest (md)
            /// Takes the strData referenced value (const unsigned char *d) and hashes it into a reference to the digest parameter.
                _ = strData.withUnsafeBytes {
                    // CommonCrypto
                    // extern unsigned char *CC_SHA256(const void *data, CC_LONG len, unsigned char *md)  -|
                    // OpenSSL                                                                             |
                    // unsigned char *SHA256(const unsigned char *d, size_t n, unsigned char *md)        <-|
                    CC_SHA256($0.baseAddress, UInt32(strData.count), &digest)
            }

                var sha256String = ""
                /// Unpack each byte in the digest array and add them to the sha256String
            for byte in digest {
                    sha256String += String(format: "%02x", UInt8(byte))
            }

                return sha256String
            }
            return ""
        }
    */

    // MARK: - Errors
        private struct TelemetryError
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
