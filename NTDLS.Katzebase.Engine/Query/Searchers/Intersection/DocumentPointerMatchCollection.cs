using NTDLS.Katzebase.Engine.Documents;

namespace NTDLS.Katzebase.Engine.Query.Searchers.Intersection
{
    internal class DocumentPointerMatch : DocumentPointer
    {
        public string? DebugSource { get; set; }

        public DocumentPointerMatch(int pageNumber, uint documentId) : base(pageNumber, documentId)
        {
        }
    }

    internal class DocumentPointerMatchCollection : Dictionary<uint, DocumentPointerMatch>
    {
        //Add the first item to the dictionary when its constructed.
        public DocumentPointerMatch Upsert(DocumentPointer documentPointer)
        {
            if (ContainsKey(documentPointer.DocumentId) == false)
            {
                var match = new DocumentPointerMatch(documentPointer.PageNumber, documentPointer.DocumentId);
                Add(documentPointer.DocumentId, match);
                return match;
            }
            else
            {
                return this[documentPointer.DocumentId];
            }
        }
    }
}
