using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json;

namespace Katzebase.PublicLibrary.Client.Management
{
    public class Server
    {
        private readonly KatzebaseClient client;

        public Server(KatzebaseClient client)
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
            string url = $"api/Server/{client.SessionId}/Ping";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponsePing>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.Message);
            }

            client.ServerProcessId = result.ProcessId;

            return result;
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
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.Message);
            }

            return result;
        }
    }
}
