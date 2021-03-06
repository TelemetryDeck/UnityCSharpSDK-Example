using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TelemetryClient.TestApp.Scripts
{
    using static MyTelemetryStrings;

    public class AppManager : MonoBehaviour
    {
        private const string githubRepoURL = "https://github.com/TelemetryDeck/UnityCSharpSDK-Example/";

        [Header("Preferences")]
        private bool initializeTelemetryOnAwake = false;

        [Header("UI")]
        [SerializeField]
        private Text uiText;
        [SerializeField]
        private Button initTelemetryButton;
        [SerializeField]
        private Button sendSimpleButton;
        [SerializeField]
        private Button sendAdvancedButton;
        [SerializeField]
        private Button startNewSessionButton;
        [SerializeField]
        private Button stopTelemetryButton;

        private int numberOfSignalsSentThisSession = 0;
        private bool TelemetryInitialized => TelemetryManager.IsInitialized;

        private void Awake()
        {
            if (initializeTelemetryOnAwake)
                InitializeTelemetryIfNeeded();
        }

        private void Start()
        {
            UpdateUI();
        }

        /// <summary>
        /// Setup the Telemetry Manager so we can begin sending Signals.
        /// You may want to put this inside your project's [RuntimeInitializeOnLoadMethod].
        /// </summary>
        public void InitializeTelemetryIfNeeded()
        {
            if (TelemetryInitialized)
                return;

            var configuration = new TelemetryManagerConfiguration(TelemetryAppId);
            // anonymize the telemetry sent entirely by setting a generic user ID
            configuration.defaultUser = GenericUserId;
            // mark all signals as testing signals
            configuration.IsTestMode = true;
            configuration.showDebugLogs = true;
            // initialize the TelemetryClient (otherwise we can't send any Signals)
            // you can delay this call to whenever you choose to start sending Signals
            // TelemetryClient will automatically attempt to send a "new session" signal.
            TelemetryManager.Initialize(configuration);
            UpdateUI();
        }

        /// <summary>
        /// Updates the UI to reflect the Telemetry state.
        /// </summary>
        private void UpdateUI()
        {
            if (TelemetryInitialized)
            {
                string newText = "Telemetry initialized\n";
                newText += string.Format("Sent {0:d} signals this session.", numberOfSignalsSentThisSession);
                uiText.text = newText;
            }
            else
            {
                uiText.text = "Telemetry unavailable";
            }

            initTelemetryButton.interactable = !TelemetryInitialized;
            sendSimpleButton.interactable = TelemetryInitialized;
            sendAdvancedButton.interactable = TelemetryInitialized;
            startNewSessionButton.interactable = TelemetryInitialized;
            stopTelemetryButton.interactable = TelemetryInitialized;
        }

        /// <summary>
        /// Sends a Telemetry Signal with no additional parameters.
        /// The signal is sent without a unique user ID to make the statistics anonymous.
        /// </summary>
        public void SendSimpleSignal()
        {
            if (!TelemetryInitialized)
                return;

            TelemetryManager.SendSignal(SimpleSignalName, clientUser: GenericUserId);
            numberOfSignalsSentThisSession++;
            UpdateUI();
        }

        /// <summary>
        /// Sends a Telemetry Signal with an additional payload.
        /// The signal is sent without a unique user ID to make the statistics anonymous.
        /// </summary>
        public void SendAdvancedSignal()
        {
            if (!TelemetryInitialized)
                return;

            var numberString = string.Format("{0:d}", numberOfSignalsSentThisSession);
            var additionalPayload = new Dictionary<string, string>();
            additionalPayload.Add(SignalsCountName, numberString);
            TelemetryManager.SendSignal(AdvancedSignalName, clientUser: GenericUserId, additionalPayload);
            numberOfSignalsSentThisSession++;
            UpdateUI();
        }

        public void StartNewSession()
        {
            if (!TelemetryInitialized)
                return;

            TelemetryManager.Instance.GenerateNewSession();
            numberOfSignalsSentThisSession = 0;
            UpdateUI();
        }

        public void StopTelemetry()
        {
            if (!TelemetryInitialized)
                return;

            TelemetryManager.Terminate();
            UpdateUI();
        }

        public void AboutApp()
        {
            Application.OpenURL(githubRepoURL);
        }

        public void QuitApp()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
