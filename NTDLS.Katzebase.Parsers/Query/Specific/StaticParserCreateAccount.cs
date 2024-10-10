using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserCreateAccount
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Create, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Account
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var accountName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected account name, found: [ {accountName} ].");
            }
            query.AddAttribute(PreparedQuery.Attribute.AccountName, accountName);
            query.AddAttribute(PreparedQuery.Attribute.PasswordHash, KbClient.HashPassword("")); //TODO: parse password with "WITH parser".

            /*
            if (tokenizer.TryEatIfNext("with"))
            {
                var options = new ExpectedQueryAttributes
                {
                    {"pagesize", typeof(uint) }
                };

                query.AddAttributes(StaticParserAttributes.Parse(tokenizer, options));
            }
            */

            return query;
        }
    }
}
