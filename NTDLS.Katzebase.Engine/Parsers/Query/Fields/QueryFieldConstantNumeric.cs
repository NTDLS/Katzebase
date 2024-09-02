namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a numeric constant.
    /// </summary>
    public class QueryFieldConstantNumeric : IQueryField
    {
        public string Value { get; set; }

        public QueryFieldConstantNumeric(string value)
        {
            Value = value;
        }
    }
}
