using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class Conditions
    {
        public List<ConditionSubset> Subsets = new();

        public string ExpressionTemplate { get; set; } = string.Empty;

        public static Conditions Parse(string conditionsText, Dictionary<string, string> literalStrings)
        {
            int keyCounter = 0;

            var conditions = new Conditions();

            if (conditionsText.StartsWith('(') == false || conditionsText.StartsWith(')') == false)
            {
                conditionsText = $"({conditionsText})";
            }

            while (true)
            {
                string subsetKey = $"$sk:{keyCounter++}$";

                int startPos = conditionsText.LastIndexOf('(');
                if (startPos >= 0)
                {
                    int endPos = conditionsText.IndexOf(')', startPos);
                    if (endPos > startPos)
                    {
                        string subsetText = conditionsText.Substring(startPos, endPos - startPos + 1).Trim();
                        var subset = new ConditionSubset(subsetKey, subsetText.Substring(1, subsetText.Length - 2).Trim());
                        conditions.AddSubset(literalStrings, subset);
                        conditionsText = conditionsText.Replace(subsetText, subsetKey);
                    }
                }
                else
                {
                    break;
                }
            }

            conditions.ExpressionTemplate = conditionsText.Trim();

            return conditions;
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
            int keyCounter = 0;

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
                    string conditionKey = $"$ck:{keyCounter++}$";

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
                    subset.Expression = subset.Expression.Remove(startPosition, endPosition - startPosition).Insert(startPosition, conditionKey + " ");
                    subset.Conditions.Add(condition);
                    logicalConnector = LogicalConnector.None;
                }
            }

            Subsets.Add(subset);
        }
    }
}
