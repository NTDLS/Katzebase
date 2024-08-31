using NTDLS.Katzebase.Client.Exceptions;

namespace ParserV2.Expression
{
    internal class ExpressionIdentifier : IExpression
    {
        public string SchemaAlias { get; set; }
        public string Name { get; set; }

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
