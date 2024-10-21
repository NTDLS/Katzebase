using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query
{
    public static class StaticParserDropAccount
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Drop, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Account
            };

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var userName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected account username, found: [{userName}].");
            }

            query.AddAttribute(PreparedQuery.Attribute.UserName, userName);

            return query;
        }
    }
}
