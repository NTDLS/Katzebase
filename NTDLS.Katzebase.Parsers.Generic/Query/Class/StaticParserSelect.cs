using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using NTDLS.Katzebase.Parsers.Interfaces;
using static NTDLS.Katzebase.Parsers.Constants;
using NTDLS.Helpers;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserSelect<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer, Func<string, TData> parseStringToDoc, Func<string, TData> castStringToDoc)
        {
            string token;

            var query = new PreparedQuery<TData>(queryBatch, QueryType.Select);

            //Parse "TOP n".
            if (tokenizer.TryEatIfNext("top"))
            {
                query.RowLimit = tokenizer.EatGetNextEvaluated<int>();
            }

            //Parse field list.
            if (tokenizer.TryEatIfNext("*"))
            {
                //Select all fields from all schemas.
                query.DynamicSchemaFieldFilter ??= new();
            }
            else if (tokenizer.TryEatNextEndsWith(".*")) //schemaName.*
            {
                //Select all fields from given schema.
                //TODO: Looks like do we not support "select *" from than one schema.

                token = tokenizer.EatGetNext();

                query.DynamicSchemaFieldFilter ??= new();
                var starSchemaAlias = token.Substring(0, token.Length - 2); //Trim off the trailing .*
                query.DynamicSchemaFieldFilter.Add(starSchemaAlias.ToLowerInvariant());
            }
            else
            {
                query.SelectFields = StaticParserFieldList<TData>.Parse(queryBatch, tokenizer, [" from ", " into "], false, parseStringToDoc: parseStringToDoc, castStringToDoc: castStringToDoc);
            }

            //Parse "into".
            if (tokenizer.TryEatIfNext("into"))
            {
                if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var selectIntoSchema) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{selectIntoSchema}], expected: schema name.");
                }

                query.AddAttribute(PreparedQuery<TData>.QueryAttribute.TargetSchema, selectIntoSchema);

                query.QueryType = QueryType.SelectInto;
            }

            //Parse primary schema.
            if (!tokenizer.TryEatIfNext("from"))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{tokenizer.EatGetNext()}], expected: 'from'.");
            }

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{schemaName}], expected: schema name.");
            }

            if (tokenizer.TryEatIfNext("as"))
            {
                var schemaAlias = tokenizer.EatGetNext();
                query.Schemas.Add(new QuerySchema<TData>(schemaName.ToLowerInvariant(), schemaAlias.ToLowerInvariant()));
            }
            else
            {
                query.Schemas.Add(new QuerySchema<TData>(schemaName.ToLowerInvariant()));
            }

            //Parse joins.
            while (tokenizer.TryIsNext("inner"))
            {
                var joinedSchemas = StaticParserJoin<TData>.Parse(queryBatch, tokenizer, parseStringToDoc, castStringToDoc);
                query.Schemas.AddRange(joinedSchemas);
            }

            //Parse "where" clause.
            if (tokenizer.TryEatIfNext("where"))
            {
                query.Conditions = StaticParserWhere<TData>.Parse(queryBatch, tokenizer, parseStringToDoc, castStringToDoc);

                //Associate the root query schema with the root conditions.
                query.Schemas.First().Conditions = query.Conditions;
            }

            //Parse "group by".
            if (tokenizer.TryEatIfNext("group"))
            {
                if (tokenizer.TryEatIfNext("by") == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{tokenizer.EatGetNext()}], expected: 'by'.");
                }
                query.GroupFields = StaticParserGroupBy<TData>.Parse(queryBatch, tokenizer, parseStringToDoc: parseStringToDoc, castStringToDoc: castStringToDoc);
            }

            //Parse "order by".
            if (tokenizer.TryEatIfNext("order"))
            {
                if (tokenizer.TryEatIfNext("by") == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{tokenizer.EatGetNext()}], expected: 'by'.");
                }
                query.SortFields = StaticParserOrderBy<TData>.Parse(queryBatch, tokenizer);
            }

            //Parse "limit" clause.
            if (tokenizer.TryEatIfNext("offset"))
            {
                query.RowOffset = tokenizer.EatGetNextEvaluated<int>();
            }

            //----------------------------------------------------------------------------------------------------------------------------------
            // Validation
            //----------------------------------------------------------------------------------------------------------------------------------

            //Validation (field list):
            foreach (var documentIdentifier in query.SelectFields.DocumentIdentifiers)
            {
                if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                {
                    if (query.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in field list for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
                    }
                }
            }

            //Validation (conditions):
            foreach (var documentIdentifier in query.Conditions.FieldCollection.DocumentIdentifiers)
            {
                if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                {
                    if (query.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
                    }
                }
            }

            //Validation (join conditions):
            foreach (var schema in query.Schemas.Skip(1))
            {
                if (schema.Conditions != null)
                {
                    foreach (var documentIdentifier in schema.Conditions.FieldCollection.DocumentIdentifiers)
                    {
                        if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                        {
                            if (query.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                            {
                                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in join condition for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
                            }
                        }
                    }
                }
            }

            //Validation (root conditions):
            foreach (var documentIdentifier in query.Conditions.FieldCollection.DocumentIdentifiers)
            {
                if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                {
                    if (query.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
                    }
                }
            }

            return query;
        }
    }
}
