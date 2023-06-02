using System.Text;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class Conditions
    {
        public List<ConditionSubset> Subsets = new();

        public string ExpressionTemplate { get; set; } = string.Empty;

        public static string VariableToKey(string str)
        {
            return $"$:{str}$";
        }

        private static string GetNextLetter(string letter)
        {
            if (letter == string.Empty)
            {
                return "a";
            }
            char[] chars = letter.ToCharArray();
            char lastChar = chars[chars.Length - 1];
            char nextChar = (char)(lastChar + 1);

            if (nextChar > 'z')
            {
                return letter + "a";
            }
            else
            {
                chars[chars.Length - 1] = nextChar;
                return new string(chars);
            }
        }

        public static Conditions Parse(string conditionsText, Dictionary<string, string> literalStrings)
        {
            string lastLetter = string.Empty;

            var conditions = new Conditions();

            if (conditionsText.StartsWith('(') == false || conditionsText.StartsWith(')') == false)
            {
                conditionsText = $"({conditionsText})";
            }

            while (true)
            {
                int startPos = conditionsText.LastIndexOf('(');
                if (startPos >= 0)
                {
                    int endPos = conditionsText.IndexOf(')', startPos);
                    if (endPos > startPos)
                    {
                        lastLetter = GetNextLetter(lastLetter);
                        string subsetVariable = $"sk{lastLetter}"; //SK=Subset-Key

                        string subsetText = conditionsText.Substring(startPos, endPos - startPos + 1).Trim();
                        var subset = new ConditionSubset(subsetVariable, subsetText.Substring(1, subsetText.Length - 2).Trim());
                        conditions.AddSubset(literalStrings, subset);
                        conditionsText = conditionsText.Replace(subsetText, VariableToKey(subsetVariable));
                    }
                }
                else
                {
                    break;
                }
            }

            conditions.ExpressionTemplate = conditionsText.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            RemoveVariableMarkers(conditions);

            return conditions;
        }

        private static void RemoveVariableMarkers(Conditions conditions)
        {
            conditions.ExpressionTemplate = RemoveVariableMarker(conditions.ExpressionTemplate);

            foreach (var subset in conditions.Subsets)
            {
                subset.Expression = RemoveVariableMarker(subset.Expression);
            }
        }

        private static string RemoveVariableMarker(string str)
        {
            while (true)
            {
                int spos = str.IndexOf("$:"); //Subset-Key.
                if (spos >= 0)
                {
                    int epos = str.IndexOf("$", spos + 1);
                    if (epos > spos)
                    {
                        var tok = str.Substring(spos, epos - spos + 1);
                        var key = str.Substring(spos + 2, (epos - spos) - 2);
                        str = str.Replace(tok, key);
                    }
                    else break;
                }
                else break;
            }

            return str;
        }

        public ConditionSubset? SubsetByKey(string key)
        {
            return Subsets.Where(o => o.SubsetKey == key).FirstOrDefault();
        }

        public Conditions Clone()
        {
            var clone = new Conditions();

            foreach (var subset in Subsets)
            {
                var subsetClone = new ConditionSubset(subset.SubsetKey, subset.Expression);

                foreach (var condition in subset.Conditions)
                {
                    subsetClone.Conditions.Add(condition.Clone());
                }
                clone.Subsets.Add(subsetClone);
            }

            return clone;
        }

        public void AddSubset(Dictionary<string, string> literalStrings, ConditionSubset subset)
        {
            int position = 0;
            string lastLetter = string.Empty;

            LogicalConnector logicalConnector = LogicalConnector.None;

            while (true)
            {
                int startPosition = position;

                string token = Utilities.GetNextClauseToken(subset.Expression, ref position).ToLower();

                if (token == string.Empty)
                {
                    break; //Done.
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    continue;
                }
                else if (token == "and")
                {
                    logicalConnector = LogicalConnector.And;
                    continue;
                }
                else if (token == "or")
                {
                    logicalConnector = LogicalConnector.Or;
                    continue;
                }
                else
                {
                    lastLetter = GetNextLetter(lastLetter);
                    string conditionKey = $"ck{lastLetter}"; //CK=Condition-Key

                    string left = token;

                    //Logical Qualifier
                    token = Utilities.GetNextClauseToken(subset.Expression, ref position).ToLower();
                    LogicalQualifier logicalQualifier = Utilities.ParseLogicalQualifier(token);

                    //Righthand value:
                    string right = Utilities.GetNextClauseToken(subset.Expression, ref position).ToLower();

                    if (literalStrings.ContainsKey(right))
                    {
                        right = literalStrings[right];
                    }

                    int endPosition = position;
                    var condition = new Condition(subset.SubsetKey, conditionKey, logicalConnector, left, logicalQualifier, right);

                    position = 0;
                    subset.Expression = subset.Expression.Remove(startPosition, endPosition - startPosition).Insert(startPosition, VariableToKey(conditionKey) + " ");
                    subset.Conditions.Add(condition);
                    logicalConnector = LogicalConnector.None;
                }
            }

            subset.Expression = subset.Expression.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            Subsets.Add(subset);
        }
    }
}
