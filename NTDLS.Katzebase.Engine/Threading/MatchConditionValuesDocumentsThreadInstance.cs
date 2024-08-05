namespace NTDLS.Katzebase.Engine.Threading
{
    /// <summary>
    /// Thread parameters for a lookup operations. Used by a single thread.
    /// </summary>
    internal class MatchConditionValuesDocumentsThreadInstance
    {
        public MatchConditionValuesDocumentsThreadOperation Operation { get; set; }
        public int IndexPartition { get; set; }

        public MatchConditionValuesDocumentsThreadInstance(
            MatchConditionValuesDocumentsThreadOperation operation, int indexPartition)
        {
            Operation = operation;
            IndexPartition = indexPartition;
        }
    }
}
