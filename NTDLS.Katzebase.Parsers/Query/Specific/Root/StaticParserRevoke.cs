using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.Query.SupportingTypes.QuerySchema;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Parsers.Query.Specific.Root
{
    public static class StaticParserRevoke
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Revoke, tokenizer.GetCurrentLineNumber());

            query.AddAttribute(PreparedQuery.Attribute.SecurityPolicyPermission, tokenizer.EatIfNextEnum<SecurityPolicyPermission>());

            tokenizer.EatIfNext("from");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));

            tokenizer.EatIfNext("to");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var roleName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected role name, found: [{roleName}].");
            }
            query.AddAttribute(PreparedQuery.Attribute.RoleName, roleName);

            return query;
        }
    }
}
