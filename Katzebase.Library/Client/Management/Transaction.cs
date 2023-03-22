using Katzebase.Library.Payloads;
using Newtonsoft.Json;

namespace Katzebase.Library.Client.Management
{
    public class Transaction
    {
        private readonly KatzebaseClient client;

        public Transaction(KatzebaseClient client)
        {
            this.client = client;
        }

        public void Begin()
        {
            string url = $"api/Transaction/{client.SessionId}/Begin";

            using var response = client.Client.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new Exception(result == null ? "Invalid response" : result.Message);
            }
        }

        public void Commit()
        {
            string url = $"api/Transaction/{client.SessionId}/Commit";

            using var response = client.Client.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new Exception(result == null ? "Invalid response" : result.Message);
            }
        }

        public void Rollback()
        {
            string url = $"api/Transaction/{client.SessionId}/Rollback";

            using var response = client.Client.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new Exception(result == null ? "Invalid response" : result.Message);
            }
        }

    }
}
