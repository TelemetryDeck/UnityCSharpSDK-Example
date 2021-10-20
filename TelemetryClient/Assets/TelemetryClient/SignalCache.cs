using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace TelemetryClient
{
    /// <summary>
    /// A local cache for signals to be sent to the TelemetryDeck ingestion service
    ///
    /// There is no guarantee that Signals come out in the same order you put them in. This shouldn't matter though,
    /// since all Signals automatically get a `receivedAt` property with a date, allowing the server to reorder them
    /// correctly.
    ///
    /// The cache is backed up to disk so Signals are not lost if the app restarts.
    /// </summary>
    /// <typeparam name="T">A [Serializable] type.</typeparam>
    internal class SignalCache<T> where T : new()
    {
        private const int MAX_NUMBER_OF_SIGNALS_TO_SEND_IN_BATCH = 100;

        public bool showDebugLogs = false;

        private List<T> cachedSignals = new List<T>();
        //private readonly queue = DispatchQueue(label: "telemetrydeck-signal-cache", attributes: .concurrent);


        /// How many Signals are cached
        public int Count
        {
            get
            {
                //queue.sync(flags: .barrier) {
                return cachedSignals.Count;
                //}
            }
        }

        /// Insert a Signal into the cache
        public void Push(T signal)
        {
            //queue.sync(flags: .barrier) {
            cachedSignals.Add(signal);
            //}
        }

        /// Insert a number of Signals into the cache
        public void Push(IList<T> signals)
        {
            //queue.sync(flags: .barrier) {
            cachedSignals.AddRange(signals);
            //}
        }

        /// Remove a number of Signals from the cache and return them
        ///
        /// You should hold on to the signals returned by this function. If the action you are trying to do with them fails
        /// (e.g. sending them to a server) you should reinsert them into the cache with the `push` function.
        public List<T> Pop()
        {
            List<T> poppedSignals;

            //queue.sync {
            int sliceSize = Math.Min(MAX_NUMBER_OF_SIGNALS_TO_SEND_IN_BATCH, cachedSignals.Count);
            poppedSignals = cachedSignals.GetRange(0, sliceSize);
            cachedSignals.RemoveRange(0, sliceSize);
            //}

            return poppedSignals;
        }

        private string FileUrl
        {
            get
            {
                return $"{Application.persistentDataPath}/telemetrysignalcache";
            }
        }
        /// <summary>
        /// Saves the entire signal cache to disk.
        /// </summary>
        /// <exception cref="IOException">If the cache file could not be written to.</exception>
        public void BackupCache()
        {
            //queue.sync {
            try
            {
                var data = JsonUtility.ToJson(cachedSignals);
                File.WriteAllText(FileUrl, data);
                if (showDebugLogs)
                {
                    Debug.Log($"Saved Telemetry cache {data} of {cachedSignals.Count} signals");
                }
                /// After saving the cache, we need to clear our local cache otherwise
                /// it could get merged with the cache read back from disk later if
                /// it's still in memory
                cachedSignals.Clear();
            }
            catch (IOException e)
            {
                Debug.LogError("Error while saving Telemetry cache");
                throw e;
            }
            //}
        }

        /// Loads any previous signal cache from disk
        public SignalCache(bool showDebugLogs)
        {
            this.showDebugLogs = showDebugLogs;
            string cacheFilePath = FileUrl;
            //queue.sync {
            if (showDebugLogs)
                Debug.Log($"Loading Telemetry cache from: {cacheFilePath}");

            try
            {
                var data = File.ReadAllText(cacheFilePath);
                /// Loaded cache file, now delete it to stop it being loaded multiple times
                File.Delete(cacheFilePath);

                /// Decode the data into a new cache
                List<T> signals = JsonUtility.FromJson<List<T>>(data);
                if (showDebugLogs)
                {
                    Debug.Log($"Loaded {signals.Count} signals");
                }
                cachedSignals = signals;
            }
            catch
            {
                /// failed to load cache file; that's okay - maybe it has been loaded already
                /// or it hasn't been saved yet
            }
            //}
        }
    }
}
