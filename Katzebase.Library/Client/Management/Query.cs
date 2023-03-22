using Katzebase.Library.Payloads;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;

namespace Katzebase.Library.Client.Management
{
    public class Query
    {
        private KatzebaseClient client;

        public Query(KatzebaseClient client)
        {
            this.client = client;
        }

        public void Execute(string statement)
        {
            string url = $"api/Query/{client.SessionId}/Execute";

            var postContent = new StringContent(JsonConvert.SerializeObject(statement), Encoding.UTF8);

            using (var response = client.Client.PostAsync(url, postContent))
            {
                string resultText = response.Result.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
                if (result == null || result.Success == false)
                {
                    throw new Exception(result == null ? "Invalid response" : result.Message);
                }
            }
        }

    }
}
