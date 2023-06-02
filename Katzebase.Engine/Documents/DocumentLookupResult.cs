namespace Katzebase.Engine.Documents
{
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
