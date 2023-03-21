using System.Collections.Generic;
using System.Web.Http;

namespace Katzebase.Service.Controllers
{
    public class ServerController : ApiController
    {
        // GET api/Server
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [HttpGet]
        public IEnumerable<string> Schemas()
        {       
            return new string[] { "master", "model", "msdb", "tempdb" };
        }

        // GET api/Server/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/Server
        public void Post([FromBody]string value)
        {
        }

        // PUT api/Server/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/Server/5
        public void Delete(int id)
        {
        }
    }
}
