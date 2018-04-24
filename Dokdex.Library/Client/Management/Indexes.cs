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
    public class Indexes
    {
        private DokdexClient client;

        public Indexes(DokdexClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Creates an index on the given schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public void Create(string schema, Payloads.Index document)
        {
            string url = string.Format("api/Indexes/{0}/{1}/Create", client.SessionId, schema);

            var postContent = new StringContent(JsonConvert.SerializeObject(document), Encoding.UTF8);

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

        /// <summary>
        /// Checks for the existence of an index.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public bool Exists(string schema, string indexName)
        {
            string url = string.Format("api/Indexes/{0}/{1}/{2}/Exists", client.SessionId, schema, indexName);

            using (var response = client.Client.GetAsync(url))
            {
                string resultText = response.Result.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<ActionResponseBoolean>(resultText);
                if (result.Success == false)
                {
                    throw new Exception(result.Message);
                }

                return result.Value;
            }
        }
    }
}
