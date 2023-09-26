using Newtonsoft.Json;
using NTDLS.Katzebase.Exceptions;
using NTDLS.Katzebase.Payloads;
using System.Text;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbProcedureClient
    {
        private readonly KbClient client;

        public KbIndexesClient Indexes { get; set; }

        public KbProcedureClient(KbClient client)
        {
            this.client = client;
            this.Indexes = new KbIndexesClient(client);
        }

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        /// <param name="procedure"></param>
        /// <returns></returns>
        /// <exception cref="KbAPIResponseException"></exception>
        public KbQueryResult Execute(KbProcedure procedure)
        {
            string url = $"api/Procedure/{client.SessionId}/ExecuteProcedure";

            var postContent = new StringContent(JsonConvert.SerializeObject(procedure), Encoding.UTF8);

            using var response = client.Connection.PostAsync(url, postContent);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbQueryResult>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
            return result;
        }
    }
}
