namespace TelemetryClient.TestApp.Scripts
{
    /// <summary>
    /// A neat little place to store all of our immutable parameters.
    /// </summary>
    internal static class MyTelemetryStrings
    {
        /// <summary>
        /// ID for the app; provided by TelemetryDeck.
        /// You can (re-)generate this ID in the TelemetryDeck settings.
        /// </summary>
        internal const string TelemetryAppId = "EA6FEF43-00E7-419F-B084-3F9BE80EB90A";
        /// <summary>
        /// The generic identifier we send instead of a user-specific ID.
        /// This makes sure our telemetry data is anonymized from the time it's sent.
        /// You can choose if and which user identifier you want to send in your own project.
        /// </summary>
        internal const string GenericUserId = "TelemetryDeckUser";
        /// <summary>
        /// TelemetryDeck Signals need unique names so we can analyze the data 
        /// and let the TelemetryDeck app make pretty charts and graphics.
        /// </summary>
        internal const string SimpleSignalName = "simpleSignal";
        internal const string AdvancedSignalName = "advancedSignal";
        /// <summary>
        /// TelemetryDeck Signals need unique names so we can analyze the data 
        /// and let the TelemetryDeck app make pretty charts and graphics.
        /// </summary>
        internal const string SignalsCountName = "numberOfSignalsThisSession";
    }
}
