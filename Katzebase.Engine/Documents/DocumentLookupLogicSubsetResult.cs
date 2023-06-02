namespace Katzebase.Engine.Documents
{
    internal class DocumentLookupLogicSubsetResult
    {
        public DocumentLookupResults Results { get; set; }
        public string SubsetKey { get; set; }

        public DocumentLookupLogicSubsetResult(string subsetKey, DocumentLookupResults results)
        {
            SubsetKey = subsetKey;
            Results = results;
        }
    }
}
