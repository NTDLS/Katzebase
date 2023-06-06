using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query.Condition;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Query
{
    public class ParserEngine
    {
        static public PreparedQuery ParseQuery(string query)
        {
            PreparedQuery result = new PreparedQuery();

            var literalStrings = Utilities.CleanQueryText(ref query);

            int position = 0;
            string token = string.Empty;

            token = Utilities.GetNextToken(query, ref position).ToLower();

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
                token = Utilities.GetNextToken(query, ref position);
                if (token.ToLower() == "top")
                {
                    token = Utilities.GetNextToken(query, ref position);
                    int rowLimit = 0;

                    if (Int32.TryParse(token, out rowLimit) == false)
                    {
                        throw new KbParserException("Invalid query. Found [" + token + "], expected numeric row limit.");
                    }

                    result.RowLimit = rowLimit;

                    //Get schema name:
                    token = Utilities.GetNextToken(query, ref position);
                }

                if (token == string.Empty || Utilities.IsValidIdentifier(token, "/\\") == false)
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected schema name.");
                }
                result.Schema = token;

                token = Utilities.GetNextToken(query, ref position);
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
                token = Utilities.GetNextToken(query, ref position);
                if (token.ToLower() == "top")
                {
                    token = Utilities.GetNextToken(query, ref position);
                    int rowLimit = 0;

                    if (Int32.TryParse(token, out rowLimit) == false)
                    {
                        throw new KbParserException("Invalid query. Found [" + token + "], expected numeric row limit.");
                    }

                    result.RowLimit = rowLimit;

                    //Get schema name:
                    token = Utilities.GetNextToken(query, ref position);
                }

                if (token == string.Empty || Utilities.IsValidIdentifier(token, "/\\") == false)
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected schema name.");
                }
                result.Schema = token;

                token = Utilities.GetNextToken(query, ref position);
                if (token.ToLower() != "set")
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected [SET].");
                }

                result.UpsertKeyValuePairs = ParseUpsertKeyValues(query, ref position);

                token = Utilities.GetNextToken(query, ref position);
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
                token = Utilities.GetNextToken(query, ref position);
                if (token.ToLower() == "top")
                {
                    token = Utilities.GetNextToken(query, ref position);
                    int rowLimit = 0;

                    if (Int32.TryParse(token, out rowLimit) == false)
                    {
                        throw new KbParserException("Invalid query. Found [" + token + "], expected numeric row limit.");
                    }

                    result.RowLimit = rowLimit;

                    token = Utilities.GetNextToken(query, ref position); //Select the first column name for the loop below.
                }

                while (true)
                {
                    if (token.ToLower() == "from" || token == string.Empty)
                    {
                        break;
                    }

                    string fieldSchemaAlias = string.Empty;

                    if (token.Contains('.'))
                    {
                        var splitTok = token.Split('.');
                        fieldSchemaAlias = splitTok[0];
                        token = splitTok[1];
                    }

                    if (Utilities.IsValidIdentifier(token) == false && token != "*")
                    {
                        throw new KbParserException("Invalid token. Found [" + token + "] a valid identifier.");
                    }

                    result.SelectFields.Add(new QueryField(token, fieldSchemaAlias, token));

                    token = Utilities.GetNextToken(query, ref position);
                }

                if (token.ToLower() != "from")
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected [FROM].");
                }

                string sourceSchema = Utilities.GetNextToken(query, ref position);
                string schemaAlias = string.Empty;
                if (sourceSchema == string.Empty || Utilities.IsValidIdentifier(sourceSchema, ":") == false)
                {
                    throw new KbParserException("Invalid query. Found [" + token + "], expected schema name.");
                }

                token = Utilities.PeekNextToken(query, position);
                if (token != string.Empty && token.ToLower() == "as")
                {
                    Utilities.SkipNextToken(query, ref position);
                    schemaAlias = Utilities.GetNextToken(query, ref position);
                }

                result.Schemas.Add(new QuerySchema(sourceSchema, schemaAlias));

                token = Utilities.GetNextToken(query, ref position);
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

                    result.Conditions = Conditions.Parse(conditionText, literalStrings);
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
                string variableName = Utilities.GetNextToken(query, ref position);
                string variableValue = query.Substring(position);
                result.VariableValues.Add(new KbNameValuePair(variableName, variableValue));
            }
            #endregion

            if (result.UpsertKeyValuePairs != null)
            {
                foreach (var kvp in result.UpsertKeyValuePairs.Collection)
                {
                    Utility.EnsureNotNull(kvp.Key);
                    Utility.EnsureNotNull(kvp.Value?.ToString());

                    if (literalStrings.ContainsKey(kvp.Value.ToString()))
                    {
                        kvp.Value.Value = literalStrings[kvp.Value.ToString()];
                    }
                }
            }

            return result;
        }

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
                if ((token = Utilities.GetNextClauseToken(conditionsText, ref position)) == string.Empty)
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
