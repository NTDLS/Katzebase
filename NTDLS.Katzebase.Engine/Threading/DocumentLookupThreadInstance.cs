using NTDLS.Katzebase.Engine.Documents;

namespace NTDLS.Katzebase.Engine.Threading
{
    /// <summary>
    /// Thread parameters for a lookup operations. Used by a single thread.
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="documentPointer"></param>
    internal class DocumentLookupThreadInstance(DocumentLookupThreadOperation operation, DocumentPointer documentPointer)
    {
        public DocumentLookupThreadOperation Operation { get; set; } = operation;
        public DocumentPointer DocumentPointer { get; set; } = documentPointer;
        public Dictionary<string, NCalc.Expression> ExpressionCache { get; set; } = new();
    }
}
