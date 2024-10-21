using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Parsers.Query.Root
{
    public static class StaticParserDeny
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Deny, tokenizer.GetCurrentLineNumber());

            query.AddAttribute(PreparedQuery.Attribute.SecurityPolicyPermission, tokenizer.EatIfNextEnum<SecurityPolicyPermission>());

            tokenizer.EatIfNext("on");

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));

            tokenizer.EatIfNext("to");

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var roleName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected role name, found: [{roleName}].");
            }
            query.AddAttribute(PreparedQuery.Attribute.RoleName, roleName);

            if (tokenizer.TryEatIfNext("with"))
            {
                var options = new ExpectedQueryAttributes
                {
                    { PreparedQuery.Attribute.Recursive.ToString(), typeof(bool) }
                };

                query.AddAttributes(StaticParserAttributes.Parse(tokenizer, options));
            }

            return query;
        }
    }
}
