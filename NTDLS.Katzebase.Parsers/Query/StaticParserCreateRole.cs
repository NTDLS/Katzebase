using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query
{
    public static class StaticParserCreateRole
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Create, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Role
            };

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var roleName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected role name, found: [{roleName}].");
            }
            query.AddAttribute(PreparedQuery.Attribute.RoleName, roleName);

            if (tokenizer.TryEatIfNext("with"))
            {
                var options = new ExpectedQueryAttributes
                {
                    { PreparedQuery.Attribute.IsAdministrator.ToString(), typeof(bool) }
                };

                query.AddAttributes(StaticParserAttributes.Parse(tokenizer, options));
            }

            return query;
        }
    }
}
