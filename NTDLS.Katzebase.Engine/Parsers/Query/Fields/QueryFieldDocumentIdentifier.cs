using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields
{
    /// <summary>
    /// Contains the name of a schema.field or just a field name if the schema was nto specified.
    /// </summary>
    public class QueryFieldDocumentIdentifier : IQueryField
    {
        public string SchemaAlias { get; private set; }
        public string Name { get; private set; }

        public QueryFieldDocumentIdentifier(string value)
        {
            var values = value.Split('.');
            if (values.Length == 1)
            {
                SchemaAlias = string.Empty;
                Name = values[0];
                return;
            }
            else if (values.Length == 2)
            {
                SchemaAlias = values[0];
                Name = values[1];
                return;
            }

            throw new KbParserException("Multipart identifier contains an invalid number of segment: [{value}]");
        }

        public override bool Equals(object? obj)
        {
            if (obj is QueryFieldDocumentIdentifier other)
            {
                return SchemaAlias == other.SchemaAlias && Name == other.Name;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SchemaAlias, Name);
        }
    }
}
