using Katzebase.Engine.Documents;

namespace Katzebase.Engine.Query.Searchers.Intersection
{
    internal class DocumentPointerMatch : Dictionary<uint, DocumentPointer>
    {
        //Add the first item to the dictonary when its constructed.
        public DocumentPointerMatch(DocumentPointer documentPointer)
        {
            Add(documentPointer.DocumentId, documentPointer);
        }

        public void Upsert(DocumentPointer documentPointer)
        {
            if (ContainsKey(documentPointer.DocumentId) == false)
            {
                Add(documentPointer.DocumentId, documentPointer);
            }
        }
    }
}
