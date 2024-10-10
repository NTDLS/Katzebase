namespace NTDLS.Katzebase.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a numeric constant, string constant, or the name of a schema.field or just a field name if the schema was not specified.
    /// </summary>
    public interface IQueryField
    {
        string? Value { get; set; }
        string SchemaAlias { get; }
        int? ScriptLine { get; }

        public IQueryField Clone();
    }
}
