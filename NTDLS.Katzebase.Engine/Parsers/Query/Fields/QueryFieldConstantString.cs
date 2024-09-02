namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a string constant.
    /// </summary>
    public class QueryFieldConstantString : IQueryField
    {
        public string Value { get; set; }

        public QueryFieldConstantString(string value)
        {
            Value = value;
        }
    }
}
