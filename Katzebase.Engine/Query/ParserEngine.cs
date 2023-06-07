using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Query.Tokenizers;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Query
{
    public class ParserEngine
    {
        static public PreparedQuery ParseQuery(string queryText)
        {
            PreparedQuery result = new PreparedQuery();

            var query = new QueryTokenizer(queryText);

            string token = string.Empty;

            token = query.GetNextToken().ToLower();

            var queryType = QueryType.Select;

            if (Enum.TryParse<QueryType>(token, true, out queryType) == false)
            {
                throw new KbParserException("Invalid query. Found [" + token + "], expected select, insert, update or delete.");
            }

            result.QueryType = queryType;

            #region Delete.
            //--------------------------------------------------------------------------------------------------------------------------------------------
            if (queryType == QueryType.Delete)
            {
                /*
                token = query.GetNextToken();
                if (token.ToLower() == "top")
                {
                    token = query.GetNextToken();
                    int rowLimit = 0;

                    if (Int32.TryParse(token, out rowLimit) == false)
                    {
                        throw new KbParserException("Invalid query. Found [" + token + "], expected numeric row limit.");
                    }

                    result.RowLimit = rowLimit;

                    //Get schema name:
                    token = query.GetNextToken();
                }

                if (token == string.Empty || Utilities.IsValidIdentifier(token, "/\\") == false)
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected schema name.");
                }
                result.Schema = token;

                token = query.GetNextToken();
                if (token.ToLower() == "where")
                {
                    string conditionText = query.Substring(position).Trim();
                    if (conditionText == string.Empty)
                    {
                        throw new KbParserException("Invalid query. Found [" + token + "], expected list of conditions.");
                    }

                    result.Conditions = ParseConditions(conditionText, literalStrings);
                }
                else if (token != string.Empty)
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected end of statement.");
                }
                */
            }
            #endregion
            #region Update.
            //--------------------------------------------------------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Update)
            {
                /*
                token = query.GetNextToken();
                if (token.ToLower() == "top")
                {
                    token = query.GetNextToken();
                    int rowLimit = 0;

                    if (Int32.TryParse(token, out rowLimit) == false)
                    {
                        throw new KbParserException("Invalid query. Found [" + token + "], expected numeric row limit.");
                    }

                    result.RowLimit = rowLimit;

                    //Get schema name:
                    token = query.GetNextToken();
                }

                if (token == string.Empty || Utilities.IsValidIdentifier(token, "/\\") == false)
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected schema name.");
                }
                result.Schema = token;

                token = query.GetNextToken();
                if (token.ToLower() != "set")
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected [SET].");
                }

                result.UpsertKeyValuePairs = ParseUpsertKeyValues(query, ref position);

                token = query.GetNextToken();
                if (token != string.Empty && token.ToLower() != "where")
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected [WHERE] or end of statement.");
                }

                if (token.ToLower() == "where")
                {
                    string conditionText = query.Substring(position).Trim();
                    if (conditionText == string.Empty)
                    {
                        throw new KbParserException("Invalid query. Found [" + token + "], expected list of conditions.");
                    }

                    result.Conditions = ParseConditions(conditionText, literalStrings);
                }
                else if (token != string.Empty)
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected end of statement.");
                }
                */
            }
            #endregion
            #region Select.
            //--------------------------------------------------------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Select)
            {
                if (query.IsNextToken("top"))
                {
                    query.SkipNextToken();
                    result.RowLimit = query.GetNextTokenAsInt();
                }

                while (true)
                {
                    if (query.IsNextToken("from"))
                    {
                        break;
                    }

                    string fieldName = query.GetNextToken();
                    if (string.IsNullOrWhiteSpace(fieldName))
                    {
                        throw new KbParserException("Invalid token. Found whitespace expected identifer.");
                    }

                    string fieldSchemaAlias = string.Empty;
                    string fieldAlias;


                    if (fieldName.Contains('.'))
                    {
                        var splitTok = fieldName.Split('.');
                        fieldSchemaAlias = splitTok[0];
                        fieldName = splitTok[1];
                    }

                    query.SwapFieldLiteral(ref fieldName);

                    if (TokenHelpers.IsValidIdentifier(fieldName) == false && fieldName != "*")
                    {
                        throw new KbParserException("Invalid token identifier [" + fieldName + "].");
                    }

                    if (query.IsNextToken("as"))
                    {
                        query.SkipNextToken();
                        fieldAlias = query.GetNextToken();
                    }
                    else
                    {
                        fieldAlias = fieldName;
                    }

                    query.SwapFieldLiteral(ref fieldAlias);

                    result.SelectFields.Add(new QueryField(fieldName, fieldSchemaAlias, fieldAlias));

                    if (query.IsCurrentChar(','))
                    {
                        query.SkipDelimiters();
                    }
                    else
                    {
                        //We should have found a delimiter here, if not either we are done parsing or the query is malformed. The next check will find out.
                        break;
                    }
                }

                if (query.IsNextToken("from"))
                {
                    query.SkipNextToken();
                }
                else
                {
                    throw new KbParserException("Invalid query. Found [" + query.PeekNextToken() + "], expected [FROM].");
                }

                string sourceSchema = query.GetNextToken();
                string schemaAlias = string.Empty;
                if (sourceSchema == string.Empty || TokenHelpers.IsValidIdentifier(sourceSchema, ":") == false)
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected schema name.");
                }

                if (query.IsNextToken("as"))
                {
                    query.SkipNextToken();
                    schemaAlias = query.GetNextToken();
                }

                result.Schemas.Add(new QuerySchema(sourceSchema, schemaAlias));

                while (query.IsNextToken("inner"))
                {
                    query.SkipNextToken();
                    if (query.IsNextToken("join") == false)
                    {
                        throw new KbParserException("Invalid query. Found [" + query.GetNextToken() + "], expected [JOIN].");
                    }
                    query.SkipNextToken();

                    string subSchemaSchema = query.GetNextToken();
                    string subSchemaAlias = string.Empty;
                    if (subSchemaSchema == string.Empty || TokenHelpers.IsValidIdentifier(subSchemaSchema, ":") == false)
                    {
                        throw new KbParserException("Invalid query. Found [" + token + "], expected schema name.");
                    }

                    if (query.IsNextToken("as"))
                    {
                        query.SkipNextToken();
                        subSchemaAlias = query.GetNextToken();
                    }

                    token = query.GetNextToken();
                    if (token.ToLower() != "on")
                    {
                        throw new KbParserException("Invalid query. Found [" + token + "], expected ON.");
                    }

                    int joinCoOnditionsStartPosition = query.Position;

                    while (true)
                    {
                        if (query.IsNextToken(new string[] { "where", "" }))
                        {
                            break;
                        }

                        var joinLeftCondition = query.GetNextToken();
                        if (joinLeftCondition == string.Empty || TokenHelpers.IsValidIdentifier(joinLeftCondition, ".") == false)
                        {
                            throw new KbParserException("Invalid query. Found [" + joinLeftCondition + "], expected left side of join expression.");
                        }

                        int logicalQualifierPos = query.Position;

                        token = ConditionTokenizer.GetNextClauseToken(query.Text, ref logicalQualifierPos);
                        if (ConditionTokenizer.ParseLogicalQualifier(token) == LogicalQualifier.None)
                        {
                            throw new KbParserException("Invalid query. Found [" + token + "], logical qualifier.");
                        }

                        query.SkipTo(logicalQualifierPos);

                        var joinRightCondition = query.GetNextToken();
                        if (joinRightCondition == string.Empty || TokenHelpers.IsValidIdentifier(joinRightCondition, ".") == false)
                        {
                            throw new KbParserException("Invalid query. Found [" + joinRightCondition + "], expected right side of join expression.");
                        }
                    }

                    var joinConditionsText = query.Text.Substring(joinCoOnditionsStartPosition, query.Position - joinCoOnditionsStartPosition).Trim();
                    var joinConditions = Conditions.Parse(joinConditionsText, query.LiteralStrings);

                    result.Schemas.Add(new QuerySchema(subSchemaSchema, subSchemaAlias, joinConditions));
                }

                token = query.GetNextToken();
                if (token != string.Empty && token.ToLower() != "where")
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected [WHERE] or end of statement.");
                }

                if (token.ToLower() == "where")
                {
                    string conditionText = query.Remainder();
                    if (conditionText == string.Empty)
                    {
                        throw new KbParserException("Invalid query. Found [" + token + "], expected list of conditions.");
                    }

                    result.Conditions = Conditions.Parse(conditionText, query.LiteralStrings);
                }
                else if (token != string.Empty)
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected end of statement.");
                }
            }
            #endregion
            #region Set.
            //--------------------------------------------------------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Set)
            {
                //Variable 
                string variableName = query.GetNextToken();
                string variableValue = query.Remainder();
                result.VariableValues.Add(new KbNameValuePair(variableName, variableValue));
            }
            #endregion

            if (result.UpsertKeyValuePairs != null)
            {
                foreach (var kvp in result.UpsertKeyValuePairs.Collection)
                {
                    Utility.EnsureNotNull(kvp.Key);
                    Utility.EnsureNotNull(kvp.Value?.ToString());

                    if (query.LiteralStrings.ContainsKey(kvp.Value.ToString()))
                    {
                        kvp.Value.Value = query.LiteralStrings[kvp.Value.ToString()];
                    }
                }
            }

            return result;
        }

        /*
        private static UpsertKeyValues ParseUpsertKeyValues(string conditionsText, ref int position)
        {
            UpsertKeyValues keyValuePairs = new UpsertKeyValues();
            int beforeTokenPosition;

            while (true)
            {
                string token;
                beforeTokenPosition = position;
                if ((token = Utilities.GetNextToken(conditionsText, ref position)) == string.Empty)
                {
                    if (keyValuePairs.Collection.Count > 0)
                    {
                        break; //Completed successfully.
                    }
                    throw new KbParserException("Invalid query. Unexpexted end of query found.");
                }

                if (token.ToLower() == "where")
                {
                    position = beforeTokenPosition;
                    break; //Completed successfully.
                }

                var keyValue = new UpsertKeyValue();

                if (token == string.Empty || Utilities.IsValidIdentifier(token) == false)
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected identifier name.");
                }
                keyValue.Key = token;

                token = Utilities.GetNextToken(conditionsText, ref position);
                if (token != "=")
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected [=].");
                }

                if ((token = Utilities.GetNextToken(conditionsText, ref position)) == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected condition value.");
                }
                keyValue.Value.Value = token;

                keyValuePairs.Collection.Add(keyValue);
            }

            return keyValuePairs;
        }
        */

        /// <summary>
        /// Extraxts the condition text between parentheses.
        /// </summary>
        /// <param name="conditionsText"></param>
        /// <param name="endPosition"></param>
        /// <returns></returns>
        private static string GetConditionGroupExpression(string conditionsText, out int endPosition)
        {
            string resultingExpression = string.Empty;
            int position = 0;
            int nestLevel = 0;
            string token;

            while (true)
            {
                if ((token = ConditionTokenizer.GetNextClauseToken(conditionsText, ref position)) == string.Empty)
                {
                    break;
                }

                if (token == "(")
                {
                    nestLevel++;
                }
                else if (token == ")")
                {
                    nestLevel--;

                    if (nestLevel <= 0)
                    {
                        resultingExpression += $"{token} ";
                        break;
                    }
                }

                resultingExpression += $"{token} ";
            }

            resultingExpression = resultingExpression.Replace("( ", "(").Replace(" (", "(").Replace(") ", ")").Replace(" )", ")").Trim();

            endPosition = position;

            return resultingExpression;

        }
    }
}
