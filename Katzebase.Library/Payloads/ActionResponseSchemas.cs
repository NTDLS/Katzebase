using System;
using System.Collections.Generic;

namespace Katzebase.Library.Payloads
{
    public class ActionResponseSchemas : ActionResponse
    {
        public List<Schema> List { get; set; }

        public ActionResponseSchemas()
        {
            List = new List<Schema>();
        }

        public void Add(Schema value)
        {
            List.Add(value);
        }
    }
}
