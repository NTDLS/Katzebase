using System.Collections.Generic;

namespace Katzebase.Library.Payloads
{
    public class ActionResponseStrings : ActionResponse
    {
        public List<string> Values { get; set; }

        public void Add(string value)
        {
            Values.Add(value);
        }
    }
}
