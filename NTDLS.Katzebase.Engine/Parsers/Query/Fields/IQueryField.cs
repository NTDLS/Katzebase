using fs;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields
{
    /// <summary>
    /// Contains a numeric constant, string constant, or the name of a schema.field or just a field name if the schema was not specified.
    /// </summary>
    internal interface IQueryField
    {
        fstring Value { get; set; }
        string SchemaAlias { get; }

        public IQueryField Clone();
    }
}
