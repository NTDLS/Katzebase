using System.Collections.Generic;

namespace Katzebase.Library.Payloads
{
    public class KbActionResponseSchemas : KbActionResponse
    {
        public List<KbSchema> List { get; set; }

        public KbActionResponseSchemas()
        {
            List = new List<KbSchema>();
        }

        public void Add(KbSchema value)
        {
            List.Add(value);
        }
    }
}
