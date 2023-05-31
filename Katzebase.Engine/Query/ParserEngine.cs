using Katzebase.Library;
using Katzebase.Library.Client.Management;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using static Katzebase.Engine.Constants;
using static System.Collections.Specialized.BitVector32;

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

            /*
            foreach (var literalString in literalStrings)
            {
                if (result.Conditions != null)
                {
                    foreach (var kvp in result.Conditions.Collection)
                    {
                        kvp.Field = kvp.Field.Replace(literalString.Key, literalString.Value);
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
                    if (kvp.Field.StartsWith("\'") && kvp.Field.EndsWith("\'"))
                    {
                        kvp.Field = kvp.Field.Substring(1, kvp.Field.Length - 2);
                        kvp.IsKeyConstant = true;
                    }

                    if (kvp.Value.StartsWith("\'") && kvp.Value.EndsWith("\'"))
                    {
                        kvp.Value = kvp.Value.Substring(1, kvp.Value.Length - 2);
                        kvp.IsValueConstant = true;
                    }

                    //Process escape sequences:
                    kvp.Field = kvp.Field.Replace("\\'", "\'");
                    kvp.Value = kvp.Value.Replace("\\'", "\'");

                    if (kvp.Field.All(Char.IsDigit))
                    {
                        kvp.IsKeyConstant = true;
                    }
                    if (kvp.Value.All(Char.IsDigit))
                    {
                        kvp.IsValueConstant = true;
                    }
                }
            }
            */
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

            //result.Conditions?.MakeLowerCase(true);

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

        private static string GetConditionGroupExpression(string conditionsText, out int endPosition)
        {
            string resultingExpression = string.Empty;
            /*
            if (conditionsText.StartsWith("(") == false)
            {
                throw new Exception("Invalid query. Subexpression must start with an open parentheses.");
            }
            else if (conditionsText.EndsWith(")") == false)
            {
                throw new Exception("Invalid query. Subexpression must end with an open parentheses.");
            }

            //Trim the beinging and ending parentheses.
            conditionsText = conditionsText.Substring(1, conditionsText.Length - 1);
            */

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

        private static ConditionGroup ParseConditionGroup(string conditionsText, LogicalConnector groupLogicalConnector)
        {
            var result = new ConditionGroup(groupLogicalConnector);

            Console.WriteLine(conditionsText);

            if (conditionsText.Contains("("))
            {
            }


            int tokenPosition = 0;

            while (true)
            {
                LogicalConnector logicalConnector = LogicalConnector.None;
                string token = Utilities.GetNextClauseToken(conditionsText, ref tokenPosition).ToLower();
                if (token == string.Empty)
                {
                    //We are done.
                    break;
                }
                else if (token == "or")
                {
                    logicalConnector = LogicalConnector.Or;
                }
                else if (token == "and")
                {
                    logicalConnector = LogicalConnector.And;
                }
                else if (token == "(")
                {
                    Utilities.RewindTo(conditionsText, '(', ref tokenPosition);

                    conditionsText = conditionsText.Substring(tokenPosition).Trim(); //Trim-up the working expression.
                    tokenPosition = 0;

                    string groupExpression = GetConditionGroupExpression(conditionsText, out int endPosition);
                    conditionsText = conditionsText.Substring(endPosition).Trim(); //Trim-up the working expression.

                    if (groupExpression.StartsWith("(") && groupExpression.EndsWith(")"))
                    {
                        groupExpression = groupExpression.Substring(1, groupExpression.Length - 2);
                    }

                    var conditionGroup = ParseConditionGroup(groupExpression, logicalConnector);

                    var conditionSubset = new ConditionSubset();
                    conditionSubset.Groups.Add(conditionGroup);
                    result.Conditions.Add(conditionSubset);

                }
                else
                {
                    var condition = new ConditionSingle(logicalConnector, token);

                    token = Utilities.GetNextClauseToken(conditionsText, ref tokenPosition).ToLower();
                    condition.LogicalQualifier = Utilities.ParseLogicalQualifier(token);
                    if (condition.LogicalQualifier == LogicalQualifier.None)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected valid condition type (=, !=, etc.). Found [" + token + "]");
                    }

                    if ((token = Utilities.GetNextClauseToken(conditionsText, ref tokenPosition)) == string.Empty)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected condition value.");
                    }
                    condition.Value = token;

                    result.Conditions.Add(condition);

                 }
            }

            return result;
        }

        private static Conditions ParseConditionGroups(string conditionsText)
        {
            var conditions = new Conditions();

            while (true)
            {
                bool parseRemainder = true;

                LogicalConnector logicalConnector = LogicalConnector.None;
                string firstToken = Utilities.GetFirstClauseToken(conditionsText, out int tokenPosition).ToLower();
                if (firstToken == "or")
                {
                    logicalConnector = LogicalConnector.Or;
                    conditionsText = conditionsText.Substring(tokenPosition).Trim();
                }
                else if (firstToken == "and")
                {
                    logicalConnector = LogicalConnector.And;
                    conditionsText = conditionsText.Substring(tokenPosition).Trim();
                }
                else if (firstToken == string.Empty)
                {
                    //We are done.
                    break;
                }
                else if (firstToken != "(")
                {
                    parseRemainder = false;

                    //If the first character is not an open parenthesis then this is just an expression a=b OR a=c 
                    //  but just in case there is a group later on in the expression that starts with a parenthesis,
                    //  lets hunt for it before assuming this is just a simple expression.

                    int logicalConnectorPosition = -1;
                    int previousTokenPosition = tokenPosition;

                    while (true)
                    {
                        string token = Utilities.GetNextClauseToken(conditionsText, ref tokenPosition).ToLower();

                        if (token == "and" || token == "or")
                        {
                            //Keep track of the logical connector position so if we find a parenthesis after it we can move the cursor back to before the connector.
                            logicalConnectorPosition = previousTokenPosition;
                        }
                        else if (token == string.Empty)
                        {
                            //We didnt find any subexpressions:

                            string simpleGroupExpression = conditionsText.Substring(0, tokenPosition).Trim().ToLower();
                            var simpleConditionGroup = ParseConditionGroup(simpleGroupExpression, logicalConnector);
                            conditions.Groups.Add(simpleConditionGroup);

                            conditionsText = conditionsText.Substring(tokenPosition).Trim(); //Trim-up the working expression (this tuncates the entire string).
                            break;
                        }
                        else if (token == "(")
                        {
                            int trimPosition = logicalConnectorPosition;
                            if (trimPosition < 0)
                            {
                                tokenPosition = previousTokenPosition; //If we didnt have a connector, then we just move the cursor back before the open parenthesis.
                            }

                            string simpleGroupExpression = conditionsText.Substring(0, trimPosition).Trim().ToLower();
                            var simpleConditionGroup = ParseConditionGroup(simpleGroupExpression, logicalConnector);
                            conditions.Groups.Add(simpleConditionGroup);

                            conditionsText = conditionsText.Substring(trimPosition).Trim(); //Trim-up the working expression.
                            break;
                        }

                        previousTokenPosition = tokenPosition;
                    }
                }

                if (parseRemainder)
                {
                    string groupExpression = GetConditionGroupExpression(conditionsText, out int endPosition);
                    conditionsText = conditionsText.Substring(endPosition).Trim(); //Trim-up the working expression.

                    if (groupExpression.StartsWith("(") && groupExpression.EndsWith(")"))
                    {
                        groupExpression = groupExpression.Substring(1, groupExpression.Length - 2);
                    }

                    var conditionGroup = ParseConditionGroup(groupExpression, logicalConnector);
                    conditions.Groups.Add(conditionGroup);
                }
            }

            return conditions;
        }

        private static Conditions ParseConditions(string conditionsText)
        {
            conditionsText = conditionsText.Replace("( ", "(").Replace(" (", "(").Replace(") ", ")").Replace(" )", ")").Trim();
            /*
            if (conditionsText.StartsWith("(") == false)
            {
                conditionsText = $"({conditionsText})";
            }
            */

            var dbgCondition = ParseConditionGroups(conditionsText);


            return null;

            /*

            string output = string.Empty;

            do
            {
                output = conditionsText.Split('(', ')')[1];
                conditionsText = conditionsText.Replace($"({output})", "ABC");


            } while (string.IsNullOrEmpty(output) == false);



            //I think we need to split on the ORs 
            var result = new Conditions();
            var conditions = new Conditions();

            string token = string.Empty;
            LogicalConnector logicalConnector = LogicalConnector.None;

            int position = 0;

            while (true)
            {
                if ((token = Utilities.GetNextClauseToken(conditionsText, ref position)) == string.Empty)
                {
                    if (conditions.Collection.Count > 0)
                    {
                        break; //Completed successfully.
                    }
                    throw new Exception("Invalid query. Unexpected end of query found.");
                }

                if (token == "(")
                {
                    if (conditions.Collection.Count > 0)
                    {
                        result.Children.Add(conditions);
                    }
                    conditions = new Conditions();
                    conditions.LogicalConnector = logicalConnector;
                    logicalConnector = LogicalConnector.None;
                    continue;
                }
                else if (token == ")")
                {
                    continue;
                }
                if (token.ToLower() == "and")
                {
                    logicalConnector = LogicalConnector.And;
                    continue;
                }
                else if (token.ToLower() == "or")
                {
                    logicalConnector = LogicalConnector.Or;
                    continue;
                }
                else
                {
                    var condition = new Condition(logicalConnector, token);

                    if ((token = Utilities.GetNextClauseToken(conditionsText, ref position)) == string.Empty)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected condition type (=, !=, etc.).");
                    }

                    condition.LogicalQualifier = Utilities.ParseLogicalQualifier(token);
                    if (condition.LogicalQualifier == LogicalQualifier.None)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected valid condition type (=, !=, etc.). Found [" + token + "]");
                    }

                    if ((token = Utilities.GetNextClauseToken(conditionsText, ref position)) == string.Empty)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected condition value.");
                    }
                    condition.Value = token;

                    conditions.Add(condition);
                }
            }

            if (conditions.Collection.Count > 0)
            {
                result.Children.Add(conditions);
            }

            return result;
            */
        }

        /*

        private static Conditions ParseConditionsOld(string conditionsText)
        {
            int position = 0;
            return ParseConditionsOld(conditionsText, ref position, 0);
        }

        private static Conditions ParseConditionsOld(string conditionsText, ref int position, int nestLevel)
        {
            Conditions conditions = new Conditions();

            string token = string.Empty;
            LogicalConnector logicalConnector = logicalConnector.None;
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
                    logicalConnector = logicalConnector.And;
                    continue;
                }
                else if (token.ToLower() == "or")
                {
                    logicalConnector = LogicalConnector.Or;
                    continue;
                }
                else if (token.ToLower() == "(")
                {
                    Conditions nestedConditions = ParseConditionsOld(conditionsText, ref position, nestLevel + 1);

                    //if (condition == null)
                    //{
                    //    conditions.Add(nestedConditions);
                    //}
                    //else
                    //{
                        nestedConditions.LogicalConnector = logicalConnector;
                        conditions.Children.Add(nestedConditions);
                    //}
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

                    condition = new Condition(logicalConnector, token);


                    if ((token = Utilities.GetNextToken(conditionsText, ref position)) == string.Empty)
                    {
                        throw new Exception("Invalid query. Found [" + token + "], expected condition type (=, !=, etc.).");
                    }

                    condition.LogicalQualifier = Utilities.ParseLogicalQualifier(token);
                    if (condition.LogicalQualifier == LogicalQualifier.None)
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
        */

    }
}
