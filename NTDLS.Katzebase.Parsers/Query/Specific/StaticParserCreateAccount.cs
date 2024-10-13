using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserCreateAccount
    {
        internal static SupportingTypes.Query Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new SupportingTypes.Query(queryBatch, QueryType.Create, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Account
            };

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var username) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected username, found: [{username}].");
            }
            query.AddAttribute(SupportingTypes.Query.Attribute.UserName, username);

            tokenizer.EatIfNext("with");

            var options = new ExpectedQueryAttributes
            {
                { SupportingTypes.Query.Attribute.Password.ToString(), typeof(string) }
            };

            var attributes = StaticParserAttributes.Parse(tokenizer, options);

            if (attributes.TryGetValue(SupportingTypes.Query.Attribute.Password.ToString(), out var plainTextPassword))
            {
                query.AddAttribute(SupportingTypes.Query.Attribute.PasswordHash, KbClient.HashPassword(plainTextPassword.Value?.ToString() ?? string.Empty));
            }

            return query;
        }
    }
}
