using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dokdex.Library.Payloads;
using Newtonsoft.Json;

namespace Dokdex.Library.Client.Management
{
    public class Query
    {
        private DokdexClient client;

        public Query(DokdexClient client)
        {
            this.client = client;
        }

        public void Execute(string statement)
        {
            string url = string.Format("api/Query/{0}/Execute", client.SessionId);

            var postContent = new StringContent(JsonConvert.SerializeObject(statement), Encoding.UTF8);

            using (var response = client.Client.PostAsync(url, postContent))
            {
                string resultText = response.Result.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<ActionResponse>(resultText);
                if (result.Success == false)
                {
                    throw new Exception(result.Message);
                }
            }
        }

    }
}
