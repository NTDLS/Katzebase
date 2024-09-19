﻿using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using NTDLS.Katzebase.Shared;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserSelect
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token = tokenizer.EatGetNext();

            if (StaticParserUtility.IsStartOfQuery(token, out var queryType) == false)
            {
                string acceptableValues = string.Join("', '", Enum.GetValues<QueryType>().Where(o => o != QueryType.None));
                throw new KbParserException($"Invalid query. Found '{token}', expected: '{acceptableValues}'.");
            }

            var query = new PreparedQuery(queryBatch, queryType);

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
            if (tokenizer.TryEatNextEndsWith(".*")) //schemaName.*
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
                query.SelectFields = StaticParserFieldList.Parse(queryBatch, tokenizer, [" from ", " into "], false);
            }

            //Parse "into".
            if (tokenizer.TryEatIfNext("into"))
            {
                var selectIntoSchema = tokenizer.EatGetNext();
                query.AddAttribute(PreparedQuery.QueryAttribute.TargetSchema, selectIntoSchema);

                query.QueryType = QueryType.SelectInto;
            }

            //Parse primary schema.
            if (!tokenizer.TryEatIfNext("from"))
            {
                throw new KbParserException("Invalid query. Found '" + tokenizer.EatGetNext() + "', expected: 'from'.");
            }

            string sourceSchema = tokenizer.EatGetNext();
            string schemaAlias = string.Empty;
            if (!TokenizerHelpers.IsValidIdentifier(sourceSchema, ['#', ':']))
            {
                throw new KbParserException("Invalid query. Found '" + sourceSchema + "', expected: schema name.");
            }

            if (tokenizer.TryEatIfNext("as"))
            {
                schemaAlias = tokenizer.EatGetNext();
            }

            query.Schemas.Add(new QuerySchema(sourceSchema.ToLowerInvariant(), schemaAlias.ToLowerInvariant()));

            //Parse joins.
            while (tokenizer.TryIsNext("inner"))
            {
                var joinedSchemas = StaticParserJoin.Parse(queryBatch, tokenizer);
                query.Schemas.AddRange(joinedSchemas);
            }

            //Parse "where" clause.
            if (tokenizer.TryEatIfNext("where"))
            {
                query.Conditions = StaticParserWhere.Parse(queryBatch, tokenizer);

                //Associate the root query schema with the root conditions.
                query.Schemas.First().Conditions = query.Conditions;
            }

            //Parse "group by".
            if (tokenizer.TryEatIfNext("group"))
            {
                if (tokenizer.TryEatIfNext("by") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.EatGetNext() + "', expected: 'by'.");
                }
                query.GroupFields = StaticParserGroupBy.Parse(queryBatch, tokenizer);
            }

            //Parse "order by".
            if (tokenizer.TryEatIfNext("order"))
            {
                if (tokenizer.TryEatIfNext("by") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.EatGetNext() + "', expected: 'by'.");
                }
                query.SortFields = StaticParserOrderBy.Parse(queryBatch, tokenizer);
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
                        throw new KbParserException($"Invalid query. Schema [{documentIdentifier.Value.SchemaAlias}] referenced in field list for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
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
                        throw new KbParserException($"Invalid query. Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
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
                                throw new KbParserException($"Invalid query. Schema [{documentIdentifier.Value.SchemaAlias}] referenced in join condition for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
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
                        throw new KbParserException($"Invalid query. Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
                    }
                }
            }

            return query;
        }
    }
}
