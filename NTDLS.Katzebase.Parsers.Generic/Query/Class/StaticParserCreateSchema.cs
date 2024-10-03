using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserCreateSchema<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
        {
            var query = new PreparedQuery<TData>(queryBatch, QueryType.Create)
            {
                SubQueryType = SubQueryType.Schema
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{schemaName}], expected: schema name.");
            }
            query.Schemas.Add(new QuerySchema<TData>(schemaName));
            query.AddAttribute(PreparedQuery<TData>.QueryAttribute.Schema, schemaName);

            if (tokenizer.TryEatIfNext("with"))
            {
                var options = new ExpectedWithOptions<TData>
                {
                    {"pagesize", typeof(uint) }
                };

                query.AddAttributes(StaticParserWithOptions.Parse<TData>(tokenizer, options));
            }

            return query;
        }
    }
}
