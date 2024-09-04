using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.Generic;
using NTDLS.Katzebase.Engine.Query.Constraints;
using NTDLS.Katzebase.Engine.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Query.Tokenizers;
using NTDLS.Katzebase.Shared;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class.Select
{
    internal static class StaticSelectParser
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token = tokenizer.GetNext();

            if (StaticQueryParser.IsStartOfQuery(token, out var queryType) == false)
            {
                string acceptableValues = string.Join("', '", Enum.GetValues<QueryType>().Where(o => o != QueryType.None));
                throw new KbParserException($"Invalid query. Found '{token}', expected: '{acceptableValues}'.");
            }

            var result = new PreparedQuery(queryBatch, queryType);

            #region Parse "TOP n".

            if (tokenizer.TryIsNextToken("top"))
            {
                result.RowLimit = tokenizer.GetNextEvaluated<int>();
            }

            #endregion

            #region Parse field list.

            if (tokenizer.TryIsNextToken("*"))
            {
                //Select all fields from all schemas.
                result.DynamicSchemaFieldFilter ??= new();
            }
            if (tokenizer.TryNextTokenEndsWith(".*")) //schemaName.*
            {
                //Select all fields from given schema.
                //TODO: Looks like do we not support "select *" from than one schema.

                token = tokenizer.GetNext();

                result.DynamicSchemaFieldFilter ??= new();
                var starSchemaAlias = token.Substring(0, token.Length - 2); //Trim off the trailing .*
                result.DynamicSchemaFieldFilter.Add(starSchemaAlias.ToLowerInvariant());
            }
            else
            {
                result.SelectFields = StaticSelectFieldParser.ParseSelectFields(queryBatch, tokenizer);
            }

            #endregion

            #region Parse "into".

            if (tokenizer.TryIsNextToken("into"))
            {
                var selectIntoSchema = tokenizer.GetNext();
                result.AddAttribute(PreparedQuery.QueryAttribute.TargetSchema, selectIntoSchema);

                result.QueryType = QueryType.SelectInto;
            }

            #endregion

            #region Parse primary schema.

            if (!tokenizer.TryIsNextToken("from"))
            {
                throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: 'from'.");
            }

            string sourceSchema = tokenizer.GetNext();
            string schemaAlias = string.Empty;
            if (!TokenHelpers.IsValidIdentifier(sourceSchema, ['#', ':']))
            {
                throw new KbParserException("Invalid query. Found '" + sourceSchema + "', expected: schema name.");
            }

            if (tokenizer.TryIsNextToken("as"))
            {
                schemaAlias = tokenizer.GetNext();
            }

            result.Schemas.Add(new QuerySchema(sourceSchema.ToLowerInvariant(), schemaAlias.ToLowerInvariant()));

            #endregion

            #region Parse joins.

            while (tokenizer.TryIsNextToken("inner"))
            {
                if (tokenizer.TryIsNextToken("join") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: 'join'.");
                }

                string subSchemaSchema = tokenizer.GetNext();
                string subSchemaAlias = string.Empty;
                if (!TokenHelpers.IsValidIdentifier(subSchemaSchema, ':'))
                {
                    throw new KbParserException("Invalid query. Found '" + subSchemaSchema + "', expected: schema name.");
                }

                if (tokenizer.TryIsNextToken("as"))
                {
                    subSchemaAlias = tokenizer.GetNext();
                }
                else
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: 'as' (schema alias).");
                }

                token = tokenizer.GetNext();
                if (!token.Is("on"))
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected 'on'.");
                }

                int joinConditionsStartPosition = tokenizer.Caret;

                while (true)
                {
                    if (tokenizer.InertTryIsNextToken(["where", "order", "inner", ""]))
                    {
                        //Found start of next part of query.
                        break;
                    }

                    if (tokenizer.InertTryCompareNextToken((o) => StaticQueryParser.IsStartOfQuery(o)))
                    {
                        //Found start of next query.
                        break;
                    }

                    if (tokenizer.InertTryIsNextToken(["and", "or"]))
                    {
                        tokenizer.SkipNext();
                    }

                    var joinLeftCondition = tokenizer.GetNext();
                    if (!TokenHelpers.IsValidIdentifier(joinLeftCondition, '.'))
                    {
                        throw new KbParserException("Invalid query. Found '" + joinLeftCondition + "', expected: left side of join expression.");
                    }

                    int logicalQualifierPos = tokenizer.Caret;

                    token = ConditionTokenizer.GetNext(tokenizer.Text, ref logicalQualifierPos);
                    if (ConditionTokenizer.ParseLogicalQualifier(token) == LogicalQualifier.None)
                    {
                        throw new KbParserException("Invalid query. Found '" + token + "], expected logical qualifier.");
                    }

                    tokenizer.SetCaret(logicalQualifierPos);

                    var joinRightCondition = tokenizer.GetNext();
                    if (!TokenHelpers.IsValidIdentifier(joinRightCondition, '.'))
                    {
                        throw new KbParserException("Invalid query. Found '" + joinRightCondition + "', expected: right side of join expression.");
                    }
                }

                var joinConditionsText = tokenizer.Text.Substring(joinConditionsStartPosition, tokenizer.Caret - joinConditionsStartPosition).Trim();
                var joinConditions = Conditions.Create(queryBatch, joinConditionsText, tokenizer, subSchemaAlias);

                result.Schemas.Add(new QuerySchema(subSchemaSchema.ToLowerInvariant(), subSchemaAlias.ToLowerInvariant(), joinConditions));
            }

            #endregion

            #region Parse "where" clause.

            if (tokenizer.TryIsNextToken("where"))
            {
                //Look for tokens that would mean the end of the where clause
                if (tokenizer.InertTryGetNextIndexOf([" group ", " order "], out int endOfWhere) == false)
                {
                    //Maybe we end at the next query?
                    if (tokenizer.InertTryGetNextIndexOf((o) => StaticQueryParser.IsStartOfQuery(o), out endOfWhere) == false)
                    {
                        //Well, I suppose we will take the remainder of the query text.
                        endOfWhere = tokenizer.Length;
                    }
                }

                string conditionText = tokenizer.SubStringAbsolute(endOfWhere).Trim();
                if (conditionText == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + conditionText + "', expected: list of conditions.");
                }

                result.Conditions = Conditions.Create(queryBatch, conditionText, tokenizer);

                //Associate the root query schema with the root conditions.
                result.Schemas.First().Conditions = result.Conditions;
            }

            #endregion

            #region Parse "group by".

            if (tokenizer.TryIsNextToken("group"))
            {
                if (tokenizer.TryIsNextToken("by") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: 'by'.");
                }
                tokenizer.SkipNext();

                //TODO: Reimplement group by parser
                //result.GroupFields = StaticFunctionParsers.ParseGroupByFields(tokenizer);
            }

            #endregion

            #region Parse "order by".

            if (tokenizer.TryIsNextToken("order"))
            {
                if (tokenizer.TryIsNextToken("by") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.GetNext() + "', expected: 'by'.");
                }

                var fields = new List<string>();

                while (tokenizer.IsEnd() == false)
                {
                    if (tokenizer.InertTryCompareNextToken((o) => StaticQueryParser.IsStartOfQuery(o)))
                    {
                        //Found start of next query.
                        break;
                    }

                    var fieldToken = tokenizer.GetNext([',']);

                    if (fieldToken == string.Empty)
                    {
                        continue;
                    }

                    if (result.SortFields.Count > 0)
                    {
                        if (tokenizer.IsNextCharacter(','))
                        {
                            fieldToken = tokenizer.GetNext();
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
                            throw new KbParserException("Invalid query. Found '" + tokenizer.SubString() + "', expected: end of statement.");
                        }

                        break;
                    }

                    var sortDirection = KbSortDirection.Ascending;
                    if (tokenizer.TryIsNextToken(["asc", "desc"]))
                    {
                        if (tokenizer.GetNext().Is("desc"))
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
