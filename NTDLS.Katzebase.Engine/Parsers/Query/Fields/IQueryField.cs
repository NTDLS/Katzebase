namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields
{
    internal interface IQueryField
    {
        string Value { get; set; }
        string SchemaAlias { get; }
    }
}
