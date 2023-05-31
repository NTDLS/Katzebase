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

            var literalStrings = Utilities.CleanQueryText(ref query);

            //var literalStrings = Utilities.SwapOutLiteralStrings(ref query);

            int position = 0;
            string token = string.Empty;

            token = Utilities.GetNextToken(query, ref position).ToLower();

            var queryType = QueryType.Select;

            if (Enum.TryParse<QueryType>(token, true, out queryType) == false)
            {
                throw new Exception("Invalid query. Found [" + token + "], expected select, insert, update or delete.");
            }

            result.QueryType = queryType;
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

                    result.Conditions = ParseConditions(conditionText, literalStrings);
                }
                else if (token != string.Empty)
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected end of statement.");
                }
                */
            }
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

                    result.Conditions = ParseConditions(conditionText, literalStrings);
                }
                else if (token != string.Empty)
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected end of statement.");
                }
                */
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

                    result.Conditions = ParseConditions(conditionText, literalStrings);
                }
                else if (token != string.Empty)
                {
                    throw new Exception("Invalid query. Found [" + token + "], expected end of statement.");
                }
            }
            //--------------------------------------------------------------------------------------------------------------------------------------------

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
                    throw new Exception("Invalid query. Unexpexted end of query found.");
                }

                if (token.ToLower() == "where")
                {
                    position = beforeTokenPosition;
                    break; //Completed successfully.
                }

                var keyValue = new UpsertKeyValue();

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

        /// <summary>
        /// Parses the individual conditions in a group. 
        /// </summary>
        /// <param name="conditionsText"></param>
        /// <param name="groupLogicalConnector"></param>
        /// <param name="literalStrings"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static ConditionGroup ParseConditionGroup(string conditionsText, LogicalConnector groupLogicalConnector, Dictionary<string, string> literalStrings)
        {
            var result = new ConditionGroup(groupLogicalConnector);

            int tokenPosition = 0;

            LogicalConnector logicalConnector = LogicalConnector.None;

            while (true)
            {
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

                    var conditionGroup = ParseConditionGroup(groupExpression, logicalConnector, literalStrings);
                    var conditionSubset = new ConditionSubset(logicalConnector);
                    conditionSubset.Conditions = conditionGroup.Conditions;
                    result.Conditions.Add(conditionSubset);

                    logicalConnector = LogicalConnector.None; //We just used this, so reset it.
                }
                else
                {
                    var condition = new ConditionSingle(logicalConnector, token);
                    logicalConnector = LogicalConnector.None; //We just used this, so reset it.

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
                    condition.Right.Value = token;

                    if (literalStrings.ContainsKey(token))
                    {
                        condition.Right.Value = literalStrings[token];
                    }

                    result.Conditions.Add(condition);

                }
            }

            return result;
        }

        /// <summary>
        /// Parses the groups of conditions (each set nested in parenthisis). If there are no groups, then a single one is asssumed.
        /// </summary>
        /// <param name="conditionsText"></param>
        /// <param name="literalStrings"></param>
        /// <returns></returns>
        private static Conditions ParseConditionGroups(string conditionsText, Dictionary<string, string> literalStrings)
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
                            var simpleConditionGroup = ParseConditionGroup(simpleGroupExpression, logicalConnector, literalStrings);
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
                            var simpleConditionGroup = ParseConditionGroup(simpleGroupExpression, logicalConnector, literalStrings);
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

                    var conditionGroup = ParseConditionGroup(groupExpression, logicalConnector, literalStrings);
                    conditions.Groups.Add(conditionGroup);
                }
            }

            return conditions;
        }

        /// <summary>
        /// Parses a nested set of conditions (A = 10 AND (B = 11 OR (C = 12))...
        /// </summary>
        /// <param name="conditionsText"></param>
        /// <param name="literalStrings"></param>
        /// <returns></returns>
        private static Conditions ParseConditions(string conditionsText, Dictionary<string, string> literalStrings)
        {
            conditionsText = conditionsText.Replace("( ", "(").Replace(" (", "(").Replace(") ", ")").Replace(" )", ")").Trim();
            var conditions = ParseConditionGroups(conditionsText, literalStrings);
            //DebugPrintConditions(conditions);
            return conditions;
        }

        private static void DebugPrintConditions(Conditions conditions)
        {
            foreach (var group in conditions.Groups)
            {
                Console.WriteLine(DebugLogicalConnectorToString(group.LogicalConnector) + " (");
                foreach (var condition in group.Conditions)
                {
                    DebugPrintCondition(condition, 1);
                }
                Console.WriteLine(")");
            }
        }

        private static string DebugLogicalConnectorToString(LogicalConnector logicalConnector)
        {
            return (logicalConnector == LogicalConnector.None ? "" : logicalConnector.ToString().ToUpper() + " ");
        }

        private static string DebugLogicalQualifierToString(LogicalQualifier logicalQualifier)
        {
            switch (logicalQualifier)
            {
                case LogicalQualifier.Equals:
                    return "=";
                case LogicalQualifier.NotEquals:
                    return "!=";
                case LogicalQualifier.GreaterThanOrEqual:
                    return ">=";
                case LogicalQualifier.LessThanOrEqual:
                    return "<=";
                case LogicalQualifier.LessThan:
                    return "<";
                case LogicalQualifier.GreaterThan:
                    return ">";
            }

            return "";
        }

        private static void DebugPrintCondition(ConditionBase condition, int depth)
        {
            if (condition is ConditionSubset)
            {
                Console.WriteLine("".PadLeft(depth, '\t') + "(");
                var subset = (ConditionSubset)condition;
                foreach (var subsetCondition in subset.Conditions)
                {
                    DebugPrintCondition(subsetCondition, depth + 1);
                }

                Console.WriteLine("".PadLeft(depth, '\t') + ")");
            }
            else if(condition is ConditionSingle)
            {
                var single = (ConditionSingle)condition;
                Console.Write("".PadLeft(depth, '\t') + DebugLogicalConnectorToString(single.LogicalConnector));
                Console.WriteLine($"[{single.Left}] {DebugLogicalQualifierToString(single.LogicalQualifier)} [{single.Right}]");
            }
        }
    }
}
