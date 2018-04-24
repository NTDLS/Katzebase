using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Dokdex.Library.Payloads;

namespace Dokdex.Library.Client.Management
{
    public class Schema
    {
        private DokdexClient client;

        public Indexes Indexes { get; set; }

        public Schema(DokdexClient client)
        {
            this.client = client;
            this.Indexes = new Indexes(client);
        }

        /// <summary>
        /// Creates a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        public void Create(string schema)
        {
            string url = string.Format("api/Schema/{0}/{1}/Create", client.SessionId, schema);

            using (var response = client.Client.GetAsync(url))
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
        /// Checks for the existence of a schema.
        /// </summary>
        /// <param name="schema"></param>
        public bool Exists(string schema)
        {
            string url = string.Format("api/Schema/{0}/{1}/Exists", client.SessionId, schema);

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

        /// <summary>
        /// Drops a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        public void Drop(string schema)
        {
            string url = string.Format("api/Schema/{0}/{1}/Drop", client.SessionId, schema);

            using (var response = client.Client.GetAsync(url))
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
        /// Lists the existing schemas within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        public ActionResponseSchemas List(string schema)
        {
            string url = string.Format("api/Schema/{0}/{1}/List", client.SessionId, schema);

            using (var response = client.Client.GetAsync(url))
            {
                string resultText = response.Result.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<ActionResponseSchemas>(resultText);
            }
        }
    }
}
