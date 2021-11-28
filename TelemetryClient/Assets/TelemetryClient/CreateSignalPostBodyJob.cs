using System;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Unity.Jobs;

namespace TelemetryClient
{
    internal struct CreateSignalPostBodyJob : IJob
    {
        public string userIdentifier;
        public string appID;
        public string sessionID;
        public string signalType;
        public SignalPayload signalPayload;
        public NativeArray<SignalPostBody> result;

        public void Execute()
        {
            var userHash = ComputeSha256Hash(userIdentifier, Encoding.Unicode);

            result[0] = new SignalPostBody()
            {
                receivedAt = DateTime.Now,
                appID = new Guid(appID),
                clientUser = userHash,
                sessionID = sessionID,
                type = signalType,
                payload = signalPayload.ToMultiValueDimension()
            };
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
                    // x2 means two-digit hexadecimal string
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}