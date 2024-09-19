using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserInsert
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token = tokenizer.EatGetNext();

            if (StaticParserUtility.IsStartOfQuery(token, out var queryType) == false)
            {
                string acceptableValues = string.Join("', '", Enum.GetValues<QueryType>().Where(o => o != QueryType.None));
                throw new KbParserException($"Invalid query. Found '{token}', expected: '{acceptableValues}'.");
            }

            //tokenizer.TryEatIsNext();
            //tokenizer.EatNext();

            var result = new PreparedQuery(queryBatch, queryType);




            /*
            //Parse "TOP n".
            if (tokenizer.TryEatIsNextToken("top"))
            {
                result.RowLimit = tokenizer.EatGetNextEvaluated<int>();
            }

            //Parse field list.
            if (tokenizer.TryEatIsNextToken("*"))
            {
                //Select all fields from all schemas.
                result.DynamicSchemaFieldFilter ??= new();
            }
            if (tokenizer.TryEatNextTokenEndsWith(".*")) //schemaName.*
            {
                //Select all fields from given schema.
                //TODO: Looks like do we not support "select *" from than one schema.

                token = tokenizer.EatGetNext();

                result.DynamicSchemaFieldFilter ??= new();
                var starSchemaAlias = token.Substring(0, token.Length - 2); //Trim off the trailing .*
                result.DynamicSchemaFieldFilter.Add(starSchemaAlias.ToLowerInvariant());
            }
            else
            {
                result.SelectFields = StaticParserFieldList.Parse(queryBatch, tokenizer, [" from ", " into "], false);
            }

            //Parse "into".
            if (tokenizer.TryEatIsNextToken("into"))
            {
                var selectIntoSchema = tokenizer.EatGetNext();
                result.AddAttribute(PreparedQuery.QueryAttribute.TargetSchema, selectIntoSchema);

                result.QueryType = QueryType.SelectInto;
            }

            //Parse primary schema.
            if (!tokenizer.TryEatIsNextToken("from"))
            {
                throw new KbParserException("Invalid query. Found '" + tokenizer.EatGetNext() + "', expected: 'from'.");
            }

            string sourceSchema = tokenizer.EatGetNext();
            string schemaAlias = string.Empty;
            if (!TokenizerHelpers.IsValidIdentifier(sourceSchema, ['#', ':']))
            {
                throw new KbParserException("Invalid query. Found '" + sourceSchema + "', expected: schema name.");
            }

            if (tokenizer.TryEatIsNextToken("as"))
            {
                schemaAlias = tokenizer.EatGetNext();
            }

            result.Schemas.Add(new QuerySchema(sourceSchema.ToLowerInvariant(), schemaAlias.ToLowerInvariant()));

            //Parse joins.
            while (tokenizer.TryIsNextToken("inner"))
            {
                var joinedSchemas = StaticParserJoin.Parse(queryBatch, tokenizer);
                result.Schemas.AddRange(joinedSchemas);
            }

            //Parse "where" clause.
            if (tokenizer.TryEatIsNextToken("where"))
            {
                result.Conditions = StaticParserWhere.Parse(queryBatch, tokenizer);

                //Associate the root query schema with the root conditions.
                result.Schemas.First().Conditions = result.Conditions;
            }

            //Parse "group by".
            if (tokenizer.TryEatIsNextToken("group"))
            {
                if (tokenizer.TryEatIsNextToken("by") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.EatGetNext() + "', expected: 'by'.");
                }
                result.GroupFields = StaticParserGroupBy.Parse(queryBatch, tokenizer);
            }

            //Parse "order by".
            if (tokenizer.TryEatIsNextToken("order"))
            {
                if (tokenizer.TryEatIsNextToken("by") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.EatGetNext() + "', expected: 'by'.");
                }
                result.SortFields = StaticParserOrderBy.Parse(queryBatch, tokenizer);
            }

            //Parse "limit" clause.
            if (tokenizer.TryEatIsNextToken("offset"))
            {
                result.RowOffset = tokenizer.EatGetNextEvaluated<int>();
            }

            //----------------------------------------------------------------------------------------------------------------------------------
            // Validation
            //----------------------------------------------------------------------------------------------------------------------------------

            //Validation (field list):
            foreach (var documentIdentifier in result.SelectFields.DocumentIdentifiers)
            {
                if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                {
                    if (result.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        throw new KbParserException($"Invalid query. Schema [{documentIdentifier.Value.SchemaAlias}] referenced in field list for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
                    }
                }
            }

            //Validation (conditions):
            foreach (var documentIdentifier in result.Conditions.FieldCollection.DocumentIdentifiers)
            {
                if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                {
                    if (result.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        throw new KbParserException($"Invalid query. Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
                    }
                }
            }

            //Validation (join conditions):
            foreach (var schema in result.Schemas.Skip(1))
            {
                if (schema.Conditions != null)
                {
                    foreach (var documentIdentifier in schema.Conditions.FieldCollection.DocumentIdentifiers)
                    {
                        if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                        {
                            if (result.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                            {
                                throw new KbParserException($"Invalid query. Schema [{documentIdentifier.Value.SchemaAlias}] referenced in join condition for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
                            }
                        }
                    }
                }
            }

            //Validation (root conditions):
            foreach (var documentIdentifier in result.Conditions.FieldCollection.DocumentIdentifiers)
            {
                if (string.IsNullOrEmpty(documentIdentifier.Value.SchemaAlias) == false)
                {
                    if (result.Schemas.Any(o => o.Prefix.Is(documentIdentifier.Value.SchemaAlias)) == false)
                    {
                        throw new KbParserException($"Invalid query. Schema [{documentIdentifier.Value.SchemaAlias}] referenced in condition for [{documentIdentifier.Value.FieldName}] does not exist in the query.");
                    }
                }
            }
            */

            return result;
        }
    }
}
