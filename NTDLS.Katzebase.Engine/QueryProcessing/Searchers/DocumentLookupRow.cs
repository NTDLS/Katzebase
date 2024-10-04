namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal class DocumentLookupRow
    {
        public List<string?> Values { get; private set; }

        public DocumentLookupRow(List<string?> values)
        {
            Values = values;
        }

        public DocumentLookupRow()
        {
            Values = new();
        }
    }
}
