using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserDropAccount
    {
        internal static SupportingTypes.Query Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new SupportingTypes.Query(queryBatch, QueryType.Drop, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Account
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var userName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected account username, found: [{userName}].");
            }

            query.AddAttribute(SupportingTypes.Query.Attribute.UserName, userName);

            return query;
        }
    }
}
