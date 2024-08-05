using NTDLS.Katzebase.Engine.Documents;

namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{
    /// <summary>
    /// Thread parameters for a lookup operations. Used by a single thread.
    /// </summary>
    internal class RebuildIndexThreadParameterInstance
    {
        public RebuildIndexThreadOperation Operation { get; set; }
        public DocumentPointer DocumentPointer { get; set; }

        public RebuildIndexThreadParameterInstance(RebuildIndexThreadOperation operation, DocumentPointer documentPointer)
        {
            Operation = operation;
            DocumentPointer = documentPointer;
        }
    }
}
