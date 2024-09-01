using NTDLS.Katzebase.Client.Exceptions;

namespace ParserV2.Parsers.Query.Expressions.Fields
{
    /// <summary>
    /// Contains the name of a schema.field or just a field name if the schema was nto specified.
    /// </summary>
    internal class ExpressionIdentifier : IExpression
    {
        public string SchemaAlias { get; private set; }
        public string Name { get; private set; }

        public ExpressionIdentifier(string value)
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
    }
}
