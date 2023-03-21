using System.Collections.Generic;

namespace Katzebase.Library.Payloads
{
    public class KbActionResponseStrings : KbActionResponse
    {
        public List<string> Values { get; set; }

        public void Add(string value)
        {
            Values.Add(value);
        }
    }
}
