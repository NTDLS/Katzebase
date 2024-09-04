using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using NTDLS.Katzebase.Shared;
using static NTDLS.Katzebase.Client.KbConstants;
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

            var result = new PreparedQuery(queryBatch, queryType);

            #region Parse "TOP n".

            if (tokenizer.TryEatIsNextToken("top"))
            {
                result.RowLimit = tokenizer.EatGetNextEvaluated<int>();
            }

            #endregion

            #region Parse field list.

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
                result.SelectFields = StaticParserFieldList.Parse(queryBatch, tokenizer, [" from ", " into "]);
            }

            #endregion

            #region Parse "into".

            if (tokenizer.TryEatIsNextToken("into"))
            {
                var selectIntoSchema = tokenizer.EatGetNext();
                result.AddAttribute(PreparedQuery.QueryAttribute.TargetSchema, selectIntoSchema);

                result.QueryType = QueryType.SelectInto;
            }

            #endregion

            #region Parse primary schema.

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

            #endregion

            #region Parse joins.

            while (tokenizer.TryIsNextToken("inner"))
            {
                var joinedSchemas = StaticParserJoin.Parse(queryBatch, tokenizer);
                result.Schemas.AddRange(joinedSchemas);
            }

            #endregion

            #region Parse "where" clause.

            if (tokenizer.TryEatIsNextToken("where"))
            {
                result.Conditions = StaticParserWhere.Parse(queryBatch, tokenizer);

                //Associate the root query schema with the root conditions.
                result.Schemas.First().Conditions = result.Conditions;
            }

            #endregion

            #region Parse "group by".

            if (tokenizer.TryEatIsNextToken("group"))
            {
                if (tokenizer.TryEatIsNextToken("by") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.EatGetNext() + "', expected: 'by'.");
                }

                StaticParserGroupBy.Parse(queryBatch, tokenizer);

                //TODO: Reimplement group by parser
                //result.GroupFields = StaticFunctionParsers.ParseGroupByFields(tokenizer);
            }

            #endregion

            #region Parse "order by".

            if (tokenizer.TryEatIsNextToken("order"))
            {
                if (tokenizer.TryEatIsNextToken("by") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.EatGetNext() + "', expected: 'by'.");
                }

                var fields = new List<string>();

                while (!tokenizer.IsExausted())
                {
                    if (tokenizer.TryCompareNextToken((o) => StaticParserUtility.IsStartOfQuery(o)))
                    {
                        //Found start of next query.
                        break;
                    }

                    var fieldToken = tokenizer.EatGetNext([',']);

                    if (fieldToken == string.Empty)
                    {
                        continue;
                    }

                    if (result.SortFields.Count > 0)
                    {
                        if (tokenizer.IsNextCharacter(','))
                        {
                            fieldToken = tokenizer.EatGetNext();
                        }
                        else if (tokenizer.Caret < tokenizer.Length) //We should have consumed the entire query at this point.
                        {
                            throw new KbParserException("Invalid query. Found '" + fieldToken + "', expected: ','.");
                        }
                    }

                    if (fieldToken == string.Empty)
                    {
                        if (tokenizer.Caret < tokenizer.Length)
                        {
                            throw new KbParserException("Invalid query. Found '" + tokenizer.EatRemainder() + "', expected: end of statement.");
                        }

                        break;
                    }

                    var sortDirection = KbSortDirection.Ascending;
                    if (tokenizer.TryEatIsNextToken(["asc", "desc"]))
                    {
                        if (tokenizer.EatGetNext().Is("desc"))
                        {
                            sortDirection = KbSortDirection.Descending;
                        }
                    }

                    result.SortFields.Add(fieldToken, sortDirection);
                }
            }

            #endregion

            return result;
        }

    }
}
