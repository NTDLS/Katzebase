using Katzebase.Engine.Documents;

namespace Katzebase.Engine.Query.Searchers.MultiSchema.Intersection
{
    internal class DocumentPointerMatch : Dictionary<uint, DocumentPointer>
    {
        //Add the first item to the dictonary when its constructed.
        public DocumentPointerMatch(DocumentPointer documentPointer)
        {
            this.Add(documentPointer.DocumentId, documentPointer);
        }

        public void Upsert(DocumentPointer documentPointer)
        {
            if (this.ContainsKey(documentPointer.DocumentId) == false)
            {
                this.Add(documentPointer.DocumentId, documentPointer);
            }
        }
    }
}
