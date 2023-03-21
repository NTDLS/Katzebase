﻿using Newtonsoft.Json;
using System;
using Katzebase.Library.Payloads;

namespace Katzebase.Library.Client.Management
{
    public class Transaction
    {
        private KatzebaseClient client;

        public Transaction(KatzebaseClient client)
        {
            this.client = client;
        }

        public void Begin()
        {
            string url = string.Format("api/Transaction/{0}/Begin", client.SessionId);

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

        public void Commit()
        {
            string url = string.Format("api/Transaction/{0}/Commit", client.SessionId);

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

        public void Rollback()
        {
            string url = string.Format("api/Transaction/{0}/Rollback", client.SessionId);

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

    }
}
