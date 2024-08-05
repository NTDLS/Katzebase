namespace NTDLS.Katzebase.Engine.Threading
{
    /// <summary>
    /// Thread parameters for a index operations. Used by a single thread.
    /// </summary>
    internal class MatchWorkingSchemaDocumentsThreadInstance
    {
        public MatchWorkingSchemaDocumentsThreadOperation Operation { get; set; }
        public int IndexPartition { get; set; }

        public MatchWorkingSchemaDocumentsThreadInstance(MatchWorkingSchemaDocumentsThreadOperation operation, int indexPartition)
        {
            Operation = operation;
            IndexPartition = indexPartition;
        }
    }
}
