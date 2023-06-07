namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// We may kill this class in favor for a better one that has a collection of dictonaries.
    /// </summary>
    internal class DocumentLookupResult
    {
        public Guid RID { get; set; }
        public List<string> Values { get; set; } = new List<string>();

        public DocumentLookupResult(Guid rid)
        {
            RID = rid;
        }
    }
}
