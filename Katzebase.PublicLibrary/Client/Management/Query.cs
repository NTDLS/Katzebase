using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json;
using System.Text;

namespace Katzebase.PublicLibrary.Client.Management
{
    public class Query
    {
        private readonly KatzebaseClient client;

        public Query(KatzebaseClient client)
        {
            this.client = client;
        }

        public KbQueryResult ExecuteQuery(string statement)
        {
            string url = $"api/Query/{client.SessionId}/ExecuteQuery";

            var postContent = new StringContent(JsonConvert.SerializeObject(statement), Encoding.UTF8);

            using var response = client.Connection.PostAsync(url, postContent);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbQueryResult>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.Message);
            }
            return result;
        }

        public KbActionResponse ExecuteNonQuery(string statement)
        {
            string url = $"api/Query/{client.SessionId}/ExecuteNonQuery";

            var postContent = new StringContent(JsonConvert.SerializeObject(statement), Encoding.UTF8);

            using var response = client.Connection.PostAsync(url, postContent);
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
