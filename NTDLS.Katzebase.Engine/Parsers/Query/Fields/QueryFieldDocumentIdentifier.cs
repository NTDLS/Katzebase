using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using fs;
namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields
{
    /// <summary>
    /// Contains the name of a schema.field or just a field name if the schema was not specified.
    /// </summary>
    internal class QueryFieldDocumentIdentifier : IQueryField
    {
        /// <summary>
        /// The qualified name of the document field, e.g. schemaName.fieldName, or just the field name if no schema was specified.
        /// </summary>
        public fstring Value { get; set; }

        /// <summary>
        /// The alias of the schema for this document field.
        /// </summary>
        public string SchemaAlias { get; private set; }

        /// <summary>
        /// The name of the document field.
        /// </summary>
        public string FieldName { get; private set; }

        public IQueryField Clone()
        {
            var clone = new QueryFieldDocumentIdentifier(Value.EnsureNotNull())
            {
                SchemaAlias = SchemaAlias,
                FieldName = FieldName,
            };

            return clone;
        }

        public QueryFieldDocumentIdentifier(string value) : this(fstring.NewS(value))
        {
            
        }
        public QueryFieldDocumentIdentifier(fstring value)
        {
            Value = fstring.NewS(value.s.Trim());

            var values = Value.s.Split('.');
            if (values.Length == 1)
            {
                SchemaAlias = string.Empty;
                FieldName = values[0];
                return;
            }
            else if (values.Length == 2)
            {
                SchemaAlias = values[0];
                FieldName = values[1];
                return;
            }

            throw new KbParserException("Multipart identifier contains an invalid number of segment: [{value}]");
        }

        public override bool Equals(object? obj)
        {
            if (obj is QueryFieldDocumentIdentifier other)
            {
                return SchemaAlias == other.SchemaAlias && FieldName == other.FieldName;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SchemaAlias, FieldName);
        }
    }
}
