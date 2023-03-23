using Katzebase.Library;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class ParserEngine
    {
        static public PreparedQuery ParseQuery(string query)
        {
            PreparedQuery result = new PreparedQuery();

            Utilities.CleanQueryText(ref query);

            var literalStrings = Utilities.SwapOutLiteralStrings(ref query);

            int position = 0;
            string token = string.Empty;

            token = Utilities.GetNextToken(query, ref position);

            QueryType queryType = QueryType.Select;

            if (Enum.TryParse<QueryType>(token, true, out queryType) == false)
            {
                throw new Exception("Invalid query. Found [" + token + "], expected select, insert, update or delete.");
            }

            result.QueryType = queryType;
            //--------------------------------------------------------------------------------------------------------------------------------------------
            if (queryType == QueryType.Delete)
            {
                token = Utilities.GetNextToken(query, ref position);
                if (token.ToLower() == "top")
                {
                    token = Utilities.GetNextToken(query, ref position);
                    int rowLimit = 0;

                    if (Int32.TryParse(token, out rowLimit) == false)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected numeric row limit.");
                    }

                    result.RowLimit = rowLimit;

                    //Get schema name:
                    token = Utilities.GetNextToken(query, ref position);
                }

                if (token == string.Empty || Utilities.IsValidIdentifier(token, "/\\") == false)
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected schema name.");
                }
                result.Schema = token;

                token = Utilities.GetNextToken(query, ref position);
                if (token.ToLower() == "where")
                {
                    string conditionText = query.Substring(position).Trim();
                    if (conditionText == string.Empty)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected list of conditions.");
                    }

                    result.Conditions = ParseConditions(conditionText);
                }
                else if (token != string.Empty)
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected end of statement.");
                }
            }
            //--------------------------------------------------------------------------------------------------------------------------------------------
            else if (queryType == QueryType.Update)
            {
                token = Utilities.GetNextToken(query, ref position);
                if (token.ToLower() == "top")
                {
                    token = Utilities.GetNextToken(query, ref position);
                    int rowLimit = 0;

                    if (Int32.TryParse(token, out rowLimit) == false)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected numeric row limit.");
                    }

                    result.RowLimit = rowLimit;

                    //Get schema name:
                    token = Utilities.GetNextToken(query, ref position);
                }

                if (token == string.Empty || Utilities.IsValidIdentifier(token, "/\\") == false)
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected schema name.");
                }
                result.Schema = token;

                token = Utilities.GetNextToken(query, ref position);
                if (token.ToLower() != "set")
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected [SET].");
                }

                result.UpsertKeyValuePairs = ParseUpsertKeyValues(query, ref position);

                token = Utilities.GetNextToken(query, ref position);
                if (token != string.Empty && token.ToLower() != "where")
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected [WHERE] or end of statement.");
                }

                if (token.ToLower() == "where")
                {
                    string conditionText = query.Substring(position).Trim();
                    if (conditionText == string.Empty)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected list of conditions.");
                    }

                    result.Conditions = ParseConditions(conditionText);
                }
                else if (token != string.Empty)
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected end of statement.");
                }
            }
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
                        throw new Exception("Invalid query. Found [" + token + "], expected numeric row limit.");
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

                    if (Utilities.IsValidIdentifier(token) == false && token != "*")
                    {
                        throw new Exception("Invalid token. Found [" + token + "] a valid identifier.");
                    }

                    result.SelectFields.Add(token);

                    token = Utilities.GetNextToken(query, ref position);
                }

                if (token.ToLower() != "from")
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected [FROM].");
                }

                token = Utilities.GetNextToken(query, ref position);
                if (token == string.Empty || Utilities.IsValidIdentifier(token, ":") == false)
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected schema name.");
                }

                result.Schema = token;

                token = Utilities.GetNextToken(query, ref position);
                if (token != string.Empty && token.ToLower() != "where")
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected [WHERE] or end of statement.");
                }

                if (token.ToLower() == "where")
                {
                    string conditionText = query.Substring(position).Trim();
                    if (conditionText == string.Empty)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected list of conditions.");
                    }

                    result.Conditions = ParseConditions(conditionText);
                }
                else if (token != string.Empty)
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected end of statement.");
                }
            }
            //--------------------------------------------------------------------------------------------------------------------------------------------

            foreach (var literalString in literalStrings)
            {
                if (result.Conditions != null)
                {
                    foreach (var kvp in result.Conditions.Collection)
                    {
                        kvp.Key = kvp.Key.Replace(literalString.Key, literalString.Value);
                        kvp.Value = kvp.Value.Replace(literalString.Key, literalString.Value);
                    }
                }

                if (result.UpsertKeyValuePairs != null)
                {
                    foreach (var kvp in result.UpsertKeyValuePairs.Collection)
                    {
                        Utility.EnsureNotNull(kvp.Key);
                        Utility.EnsureNotNull(kvp.Value);

                        kvp.Key = kvp.Key.Replace(literalString.Key, literalString.Value);
                        kvp.Value = kvp.Value.Replace(literalString.Key, literalString.Value);
                    }
                }
            }

            if (result.Conditions != null)
            {
                foreach (var kvp in result.Conditions.Collection)
                {
                    if (kvp.Key.StartsWith("\'") && kvp.Key.EndsWith("\'"))
                    {
                        kvp.Key = kvp.Key.Substring(1, kvp.Key.Length - 2);
                        kvp.IsKeyConstant = true;
                    }

                    if (kvp.Value.StartsWith("\'") && kvp.Value.EndsWith("\'"))
                    {
                        kvp.Value = kvp.Value.Substring(1, kvp.Value.Length - 2);
                        kvp.IsValueConstant = true;
                    }

                    //Process escape sequences:
                    kvp.Key = kvp.Key.Replace("\\'", "\'");
                    kvp.Value = kvp.Value.Replace("\\'", "\'");

                    if (kvp.Key.All(Char.IsDigit))
                    {
                        kvp.IsKeyConstant = true;
                    }
                    if (kvp.Value.All(Char.IsDigit))
                    {
                        kvp.IsValueConstant = true;
                    }
                }
            }

            if (result.UpsertKeyValuePairs != null)
            {
                foreach (var kvp in result.UpsertKeyValuePairs.Collection)
                {
                    Utility.EnsureNotNull(kvp.Key);
                    Utility.EnsureNotNull(kvp.Value);

                    if (kvp.Key.StartsWith("\'") && kvp.Key.EndsWith("\'"))
                    {
                        kvp.Key = kvp.Key.Substring(1, kvp.Key.Length - 2);
                        kvp.IsKeyConstant = true;
                    }

                    if (kvp.Value.StartsWith("\'") && kvp.Value.EndsWith("\'"))
                    {
                        kvp.Value = kvp.Value.Substring(1, kvp.Value.Length - 2);
                        kvp.IsValueConstant = true;
                    }

                    //Process escape sequences:
                    kvp.Key = kvp.Key.Replace("\\'", "\'");
                    kvp.Value = kvp.Value.Replace("\\'", "\'");

                    if (kvp.Key.All(Char.IsDigit))
                    {
                        kvp.IsKeyConstant = true;
                    }
                    if (kvp.Value.All(Char.IsDigit))
                    {
                        kvp.IsValueConstant = true;
                    }
                }
            }

            result.Conditions?.MakeLowerCase(true);

            return result;
        }

        private static UpsertKeyValues ParseUpsertKeyValues(string conditionsText, ref int position)
        {
            UpsertKeyValues keyValuePairs = new UpsertKeyValues();
            string token = string.Empty;

            int beforeTokenPosition = 0;

            while (true)
            {
                beforeTokenPosition = position;
                if ((token = Utilities.GetNextToken(conditionsText, ref position)) == string.Empty)
                {
                    if (keyValuePairs.Collection.Count > 0)
                    {
                        break; //Completed successfully.
                    }
                    throw new Exception("Invalid query. Unexpexted end of query found.");
                }

                if (token.ToLower() == "where")
                {
                    position = beforeTokenPosition;
                    break; //Completed successfully.
                }

                UpsertKeyValue keyValue = new UpsertKeyValue();

                if (token == string.Empty || Utilities.IsValidIdentifier(token) == false)
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected identifier name.");
                }
                keyValue.Key = token;

                token = Utilities.GetNextToken(conditionsText, ref position);
                if (token != "=")
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected [=].");
                }

                if ((token = Utilities.GetNextToken(conditionsText, ref position)) == string.Empty)
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected condition value.");
                }
                keyValue.Value = token;

                keyValuePairs.Collection.Add(keyValue);
            }

            return keyValuePairs;
        }

        private static Conditions ParseConditions(string conditionsText)
        {
            int position = 0;
            return ParseConditions(conditionsText, ref position, 0);
        }

        private static Conditions ParseConditions(string conditionsText, ref int position, int nestLevel)
        {
            Conditions conditions = new Conditions();

            string token = string.Empty;
            ConditionType conditionType = ConditionType.None;
            Condition? condition = null;

            while (true)
            {
                if ((token = Utilities.GetNextToken(conditionsText, ref position)) == string.Empty)
                {
                    if (conditions.Collection.Count > 0)
                    {
                        break; //Completed successfully.
                    }
                    throw new Exception("Invalid query. Unexpexted end of query found.");
                }

                if (token.ToLower() == "and")
                {
                    conditionType = ConditionType.And;
                    continue;
                }
                else if (token.ToLower() == "or")
                {
                    conditionType = ConditionType.Or;
                    continue;
                }
                else if (token.ToLower() == "(")
                {
                    Conditions nestedConditions = ParseConditions(conditionsText, ref position, nestLevel + 1);

                    if (condition == null)
                    {
                        conditions.Add(nestedConditions);
                    }
                    else
                    {
                        nestedConditions.ConditionType = conditionType;
                        conditions.Nest.Add(nestedConditions);
                    }
                    continue;
                }
                else if (token.ToLower() == ")")
                {
                    if (nestLevel == 0)
                    {
                        throw new Exception("Invalid query. Unexpected token [)].");
                    }
                    return conditions;
                }
                else
                {
                    if (token == string.Empty || Utilities.IsValidIdentifier(token) == false)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected identifier name.");
                    }

                    condition = new Condition(conditionType, token);


                    if ((token = Utilities.GetNextToken(conditionsText, ref position)) == string.Empty)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected condition type (=, !=, etc.).");
                    }

                    condition.ConditionQualifier = Utilities.ParseConditionQualifier(token);
                    if (condition.ConditionQualifier == ConditionQualifier.None)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected valid condition type (=, !=, etc.). Found [" + token + "]");
                    }

                    if ((token = Utilities.GetNextToken(conditionsText, ref position)) == string.Empty)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected condition value.");
                    }
                    condition.Value = token;

                    conditions.Add(condition);
                }
            }

            conditions.MakeLowerCase();

            return conditions;
        }

    }
}
