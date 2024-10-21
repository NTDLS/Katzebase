using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Query
{
    public static class StaticParserAlterSchema
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Alter, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Schema
            };

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));

            tokenizer.EatIfNext("with");
            {
                var options = new ExpectedQueryAttributes
                {
                    {"pagesize", typeof(uint) }
                };
                query.AddAttributes(StaticParserAttributes.Parse(tokenizer, options));
            }

            return query;
        }
    }
}
