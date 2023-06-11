namespace Katzebase.Engine.Query.Searchers.MultiSchema
{
    public class MSQDocumentLookupResult
    {
        public Guid RID { get; set; }
        public List<string> Values { get; set; } = new List<string>();

        public void InsertValue(int ordinal, string value)
        {
            //We do not accumulate values in the same order that they were requested by the query, we need to put them in the right place.
            Values[ordinal] = value;
        }

        //rowValues.AddValue(selectField.Ordinal, token?.ToString() ?? "");

        public MSQDocumentLookupResult(Guid rid, int fieldCount)
        {
            RID = rid;
            Values.AddRange(new string[fieldCount]);
        }
    }
}
