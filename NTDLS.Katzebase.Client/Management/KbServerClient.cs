using Newtonsoft.Json;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbServerClient
    {
        private readonly KbClient _client;

        public KbServerClient(KbClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Tests the connection to the server.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KbAPIResponseException"></exception>
        public KbActionResponsePing Ping()
        {
            string url = $"api/Server/{_client.SessionId}/Ping";

            if (string.IsNullOrWhiteSpace(_client.ClientName) == false)
            {
                url = $"api/Server/{_client.SessionId}/Ping/{_client.ClientName}";
            }

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponsePing>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }

            _client.ServerProcessId = result.ProcessId;

            return result;
        }

        /// <summary>
        /// Closes the connected process on the server and rolls back any open transactions.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KbAPIResponseException"></exception>
        public KbActionResponse CloseSession()
        {
            string url = $"api/Server/{_client.SessionId}/CloseSession";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }

            return result;
        }

        /// <summary>
        /// Terminates a process on the server and rolls back any open transactions.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KbAPIResponseException"></exception>
        public KbActionResponse TerminateProcess(ulong processId)
        {
            string url = $"api/Server/{_client.SessionId}/TerminateProcess/{processId}";

            using var response = _client.Connection.GetAsync(url);
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
