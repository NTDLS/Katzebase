using NTDLS.Katzebase.Exceptions;
using NTDLS.Katzebase.Payloads;
using Newtonsoft.Json;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbTransactionClient
    {
        private readonly KbClient client;

        public KbTransactionClient(KbClient client)
        {
            this.client = client;
        }

        public void Begin()
        {
            string url = $"api/Transaction/{client.SessionId}/Begin";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
        }

        public void Commit()
        {
            string url = $"api/Transaction/{client.SessionId}/Commit";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
        }

        public void Rollback()
        {
            string url = $"api/Transaction/{client.SessionId}/Rollback";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
        }

    }
}
