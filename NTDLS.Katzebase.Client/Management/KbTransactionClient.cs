using Newtonsoft.Json;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbTransactionClient
    {
        private readonly KbClient _client;

        public KbTransactionClient(KbClient client)
        {
            _client = client;
        }

        public void Begin()
        {
            string url = $"api/Transaction/{_client.SessionId}/Begin";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
        }

        public void Commit()
        {
            string url = $"api/Transaction/{_client.SessionId}/Commit";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
        }

        public void Rollback()
        {
            string url = $"api/Transaction/{_client.SessionId}/Rollback";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
        }

    }
}
