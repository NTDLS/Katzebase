using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Api.KbConstants;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Parsing
{
    /// <summary>
    /// ATTACH SCHEMA [schema name] FROM '[folder]'
    ///
    /// Copies a namespace (a schema folder, including all of its documents, indexes and child schemas) into the
    /// database as the given, not yet existing, schema. The source folder is not modified.
    /// </summary>
    public static class StaticParserAttachSchema
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Attach, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Schema
            };

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));
            query.AddAttribute(PreparedQuery.Attribute.Schema, schemaName);

            tokenizer.EatIfNext("from");

            query.AddAttribute(PreparedQuery.Attribute.FilePath, ParseFolderPath(tokenizer));

            return query;
        }

        /// <summary>
        /// Parses a folder path, which must be a string literal or a variable that contains a string.
        /// </summary>
        internal static string ParseFolderPath(Tokenizer tokenizer)
        {
            var token = tokenizer.EatGetNext();
            var folderPath = tokenizer.Variables.Resolve(token, out var dataType);

            if (dataType != KbBasicDataType.String || string.IsNullOrWhiteSpace(folderPath))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(),
                    $"Expected a quoted folder path, found: [{tokenizer.Variables.Resolve(token) ?? token}].");
            }

            return folderPath;
        }
    }
}
