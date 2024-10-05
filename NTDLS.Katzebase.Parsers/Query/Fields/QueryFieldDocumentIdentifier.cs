using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;

namespace NTDLS.Katzebase.Parsers.Query.Fields
{
    /// <summary>
    /// Contains the name of a schema.field or just a field name if the schema was not specified.
    /// </summary>
    public class QueryFieldDocumentIdentifier : IQueryField
    {
        /// <summary>
        /// The qualified name of the document field, e.g. schemaName.fieldName, or just the field name if no schema was specified.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The alias of the schema for this document field.
        /// </summary>
        public string SchemaAlias { get; private set; }

        /// <summary>
        /// The name of the document field.
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// If applicable, this is the line from the script that this expression is derived from.
        /// </summary>
        public int? ScriptLine { get; set; }

        public IQueryField Clone()
        {
            var clone = new QueryFieldDocumentIdentifier(ScriptLine, Value.EnsureNotNull())
            {
                SchemaAlias = SchemaAlias,
                FieldName = FieldName,
            };

            return clone;
        }

        public QueryFieldDocumentIdentifier(int? scriptLine, string value)
        {
            ScriptLine = scriptLine;
            Value = value.Trim();

            var values = Value.Split('.');
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

            throw new KbParserException($"Expected multi-part identifier, found: [{value}]");
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
