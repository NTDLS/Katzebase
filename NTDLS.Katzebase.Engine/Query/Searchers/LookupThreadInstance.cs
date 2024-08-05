using NTDLS.Katzebase.Engine.Documents;

namespace NTDLS.Katzebase.Engine.Query.Searchers
{
    /// <summary>
    /// Thread parameters for a lookup operations. Used by a single thread.
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="documentPointer"></param>
    internal class LookupThreadInstance(LookupThreadOperation operation, DocumentPointer documentPointer)
    {
        public LookupThreadOperation Operation { get; set; } = operation;
        public DocumentPointer DocumentPointer { get; set; } = documentPointer;
        public Dictionary<string, NCalc.Expression> ExpressionCache { get; set; } = new();

    }
}
