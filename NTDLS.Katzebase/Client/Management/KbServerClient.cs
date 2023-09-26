using Newtonsoft.Json;
using NTDLS.Katzebase.Exceptions;
using NTDLS.Katzebase.Payloads;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbServerClient
    {
        private readonly KbClient client;

        public KbServerClient(KbClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Tests the connection to the server.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KbAPIResponseException"></exception>
        public KbActionResponsePing Ping()
        {
            lock (client.PingLock)
            {
                string url = $"api/Server/{client.SessionId}/Ping";

                using var response = client.Connection.GetAsync(url);
                string resultText = response.Result.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<KbActionResponsePing>(resultText);
                if (result == null || result.Success == false)
                {
                    throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
                }

                client.ServerProcessId = result.ProcessId;

                return result;
            }
        }

        /// <summary>
        /// Terminates a process on the server and rolls back any open transactions.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KbAPIResponseException"></exception>
        public KbActionResponse TerminateProcess(ulong processId)
        {
            string url = $"api/Server/{client.SessionId}/TerminateProcess/{processId}";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }

            return result;
        }
    }
}
