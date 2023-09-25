﻿using NTDLS.Katzebase.Exceptions;
using NTDLS.Katzebase.Payloads;
using Newtonsoft.Json;
using System.Text;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbQueryClient
    {
        private readonly KbClient client;

        public KbQueryClient(KbClient client)
        {
            this.client = client;
        }

        public KbQueryResultCollection ExplainQuery(string statement)
        {
            string url = $"api/Query/{client.SessionId}/ExplainQuery";

            var postContent = new StringContent(JsonConvert.SerializeObject(statement), Encoding.UTF8);

            using var response = client.Connection.PostAsync(url, postContent);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbQueryResultCollection>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
            return result;
        }

        public KbQueryResultCollection ExecuteQuery(string statement)
        {
            string url = $"api/Query/{client.SessionId}/ExecuteQuery";

            var postContent = new StringContent(JsonConvert.SerializeObject(statement), Encoding.UTF8);

            using var response = client.Connection.PostAsync(url, postContent);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbQueryResultCollection>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
            return result;
        }

        public KbQueryResultCollection ExecuteQueries(List<string> statements)
        {
            string url = $"api/Query/{client.SessionId}/ExecuteQueries";

            var postContent = new StringContent(JsonConvert.SerializeObject(statements), Encoding.UTF8);

            using var response = client.Connection.PostAsync(url, postContent);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbQueryResultCollection>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
            return result;
        }

        public KbActionResponseCollection ExecuteNonQuery(string statement)
        {
            string url = $"api/Query/{client.SessionId}/ExecuteNonQuery";

            var postContent = new StringContent(JsonConvert.SerializeObject(statement), Encoding.UTF8);

            using var response = client.Connection.PostAsync(url, postContent);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponseCollection>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
            return result;
        }
    }
}
