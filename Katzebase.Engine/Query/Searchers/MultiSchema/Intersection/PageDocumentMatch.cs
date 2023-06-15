using Katzebase.Engine.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Katzebase.Engine.Query.Searchers.MultiSchema.Intersection
{
    internal class PageDocumentMatch : Dictionary<Guid, PageDocument>
    {
        //Add the first item to the dictonary when its constructed.
        public PageDocumentMatch(PageDocument pageDocument)
        {
            this.Add(pageDocument.Id, pageDocument);
        }

        public void Upsert(PageDocument pageDocument)
        {
            if (this.ContainsKey(pageDocument.Id) == false)
            {
                this.Add(pageDocument.Id, pageDocument);
            }
        }
    }
}
