using Katzebase.Engine.Documents;

namespace Katzebase.Engine.Query.Searchers.MultiSchema.Intersection
{
    internal class PageDocumentMatch : Dictionary<uint, PageDocument>
    {
        //Add the first item to the dictonary when its constructed.
        public PageDocumentMatch(PageDocument pageDocument)
        {
            this.Add(pageDocument.DocumentId, pageDocument);
        }

        public void Upsert(PageDocument pageDocument)
        {
            if (this.ContainsKey(pageDocument.DocumentId) == false)
            {
                this.Add(pageDocument.DocumentId, pageDocument);
            }
        }
    }
}
