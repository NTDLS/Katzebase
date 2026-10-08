using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Parsing
{
    /// <summary>
    /// DETACH SCHEMA [schema name] TO '[folder]'
    ///
    /// Removes a schema (including all of its documents, indexes and child schemas) from the database and moves its
    /// files to the given, not yet existing, folder, from which it can be attached to this or another server.
    /// </summary>
    public static class StaticParserDetachSchema
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Detach, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Schema
            };

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));
            query.AddAttribute(PreparedQuery.Attribute.Schema, schemaName);

            tokenizer.EatIfNext("to");

            query.AddAttribute(PreparedQuery.Attribute.FilePath, StaticParserAttachSchema.ParseFolderPath(tokenizer));

            return query;
        }
    }
}
