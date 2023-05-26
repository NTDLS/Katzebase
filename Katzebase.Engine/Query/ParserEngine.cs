using Katzebase.Library;
using Katzebase.Library.Client.Management;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// Gets a nested group of expressions from an expression. Stuff between parenthesis.
        /// </summary>
        /// <param name="conditionsText"></param>
        /// <returns></returns>
        private static string ParseExpressionGroup(string conditionsText, out int endPosition)
        {
            StringBuilder text = new();

            int nestLevel = 0;
            int position;

            for (position = 0; position < conditionsText.Length; position++)
            {
                if (conditionsText[position] == '(')
                {
                    if (nestLevel > 0)
                    {
                        text.Append(conditionsText[position]);
                    }

                    nestLevel++;
                    continue;
                }
                else if (conditionsText[position] == ')')
                {
                    nestLevel--;

                    if (nestLevel == 0)
                    {
                        endPosition = position + 1; //Skip that closing parentheses.
                        return text.ToString().Trim();
                    }

                    if (nestLevel > 0)
                    {
                        text.Append(conditionsText[position]);
                    }

                    continue;
                }
                else
                {
                    text.Append(conditionsText[position]);
                }
            }

            endPosition = position;
            return text.ToString().Trim();
        }

        private static Conditions ParseConditionGroups(string conditionsText)
        {
            Conditions conditions = new();

            while (true)
            {
                ConditionGroup conditionGroup = new();

                string token;
                int position = 0;

                var section = ParseExpressionGroup(conditionsText, out int endPosition);
                if (section == string.Empty)
                {
                    break; //We are done.
                }

                conditionsText = conditionsText.Substring(endPosition).Trim();

                LogicalConnector logicalConnector = LogicalConnector.None;

                while (true) //Parse tokens in this section of the expression.
                {
                    if ((token = Utilities.GetNextClauseToken(section, ref position)) == string.Empty)
                    {
                        if (conditionGroup.Conditions.Count > 0)
                        {
                            break; //Completed successfully.
                        }
                        throw new Exception("Invalid query. Unexpected end of query found.");
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
                    else if (token == "(")
                    {
                        int groupBeginPosition = section.LastIndexOf('(', position);
                        var subSection = ParseExpressionGroup(section.Substring(groupBeginPosition), out int subSectionEndPosition);
                        position += subSectionEndPosition - (position - groupBeginPosition);

                        Utilities.SkipWhiteSpace(section, ref position);

                        var condition = new Condition(logicalConnector, string.Empty);
                        condition.Children = ParseConditionGroups(subSection);

                        conditionGroup.Conditions.Add(condition);

                    }
                    else
                    {
                        var condition = new Condition(logicalConnector, token);

                        if ((token = Utilities.GetNextClauseToken(section, ref position)) == string.Empty)
                        {
                            throw new Exception("Invalid query. Found [" + token + "], expected condition type (=, !=, etc.).");
                        }

                        condition.LogicalQualifier = Utilities.ParseLogicalQualifier(token);
                        if (condition.LogicalQualifier == LogicalQualifier.None)
                        {
                            throw new Exception("Invalid query. Found [" + token + "], expected valid condition type (=, !=, etc.). Found [" + token + "]");
                        }

                        if ((token = Utilities.GetNextClauseToken(section, ref position)) == string.Empty)
                        {
                            throw new Exception("Invalid query. Found [" + token + "], expected condition value.");
                        }
                        condition.Value = token;
                        conditionGroup.Conditions.Add(condition);
                    }

                }

                conditions.Groups.Add(conditionGroup);
            }

            return conditions;
        }

        private static Conditions ParseConditions(string conditionsText)
        {
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
