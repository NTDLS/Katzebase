using Katzebase.PublicLibrary.Client;
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
        public bool Ping()
        {
            string url = $"api/Server/{client.SessionId}/Ping";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponseBoolean>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.Message);
            }

            return result.Value;
        }
    }
}
