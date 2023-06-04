using System.Text;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class Conditions
    {
        private string _lastLetter = string.Empty;

        public List<ConditionSubset> Subsets = new();

        /// Every condition instance starts with a single root node that all others poaint back to given some lineage. This is the name of the root node.
        public string RootExpressionKey { get; set; } = string.Empty;

        /// <summary>
        /// Every condition instance starts with a single root node that all others poaint back to given some lineage. This is the root node.
        /// </summary>
        private ConditionSubset? _root;
        public ConditionSubset Root
        {
            get
            {
                _root ??= Subsets.Where(o => o.SubsetKey == RootExpressionKey).Single();
                return _root;
            }
        }

        public IEnumerable<ConditionSubset> NonRootSubsets => Subsets.Where(o => o.SubsetKey != RootExpressionKey);

        private static string VariableToKey(string str)
        {
            return $"$:{str}$";
        }

        private string GetNextVariableLetter()
        {
            if (_lastLetter == string.Empty)
            {
                _lastLetter = "a";
                return _lastLetter;
            }
            char[] chars = _lastLetter.ToCharArray();
            char lastChar = chars[chars.Length - 1];
            char nextChar = (char)(lastChar + 1);

            if (nextChar > 'z')
            {
                _lastLetter = _lastLetter + "a";
            }
            else
            {
                chars[chars.Length - 1] = nextChar;
                _lastLetter = new string(chars);
            }

            return _lastLetter;
        }

        public static Conditions Parse(string conditionsText, Dictionary<string, string> literalStrings)
        {
            var conditions = new Conditions();
            conditions.ParseInternal(conditionsText, literalStrings);
            return conditions;
        }
        
        private void ParseInternal(string conditionsText, Dictionary<string, string> literalStrings)
        {
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
                        string subsetVariable = $"s_{GetNextVariableLetter()}"; //S=Subset-Key

                        string subsetText = conditionsText.Substring(startPos, endPos - startPos + 1).Trim();
                        var subset = new ConditionSubset(subsetVariable, subsetText.Substring(1, subsetText.Length - 2).Trim());
                        this.AddSubset(literalStrings, subset);
                        conditionsText = conditionsText.Replace(subsetText, VariableToKey(subsetVariable));
                    }
                }
                else
                {
                    break;
                }
            }

            this.RootExpressionKey = conditionsText.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            RemoveVariableMarkers();
        }

        private void RemoveVariableMarkers()
        {
            RootExpressionKey = RemoveVariableMarker(RootExpressionKey);

            foreach (var subset in Subsets)
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

        public ConditionSubset SubsetByKey(string key)
        {
            return Subsets.Where(o => o.SubsetKey == key).First();
        }

        public Conditions Clone()
        {
            var clone = new Conditions()
            {
                RootExpressionKey = this.RootExpressionKey
            };

            foreach (var subset in Subsets)
            {
                var subsetClone = new ConditionSubset(subset.SubsetKey, subset.Expression);

                foreach (var condition in subset.Conditions)
                {
                    subsetClone.Conditions.Add(condition.Clone());
                }
                subsetClone.ConditionKeys.UnionWith(subset.ConditionKeys);
                subsetClone.SubsetKeys.UnionWith(subset.SubsetKeys);

                clone.Subsets.Add(subsetClone);
            }

            return clone;
        }

        public void AddSubset(Dictionary<string, string> literalStrings, ConditionSubset subset)
        {
            int position = 0;

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
                    if (token.StartsWith("$:c"))
                    {
                        subset.ConditionKeys.Add(token.Substring(2, token.Length - 3));
                    }
                    else if (token.StartsWith("$:s"))
                    {
                        subset.SubsetKeys.Add(token.Substring(2, token.Length - 3));
                    }

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
                    string conditionKey = $"c_{GetNextVariableLetter()}"; //C=Condition-Key

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

        #region Debug.


        public string BuildFullVirtualExpression()
        {
            var result = new StringBuilder();
            result.AppendLine(RootExpressionKey);
            result.AppendLine("(");

            foreach (var subsetKey in Root.SubsetKeys)
            {
                var subExpression = SubsetByKey(subsetKey);
                result.AppendLine("  [" + subExpression.Expression + "]");
                if (subExpression.SubsetKeys.Count > 0)
                {
                    result.AppendLine("  (");
                    BuildFullVirtualExpression(ref result, subExpression, 1);
                    result.AppendLine("  )");
                }
            }

            result.AppendLine(")");

            return result.ToString();
        }

        private void BuildFullVirtualExpression(ref StringBuilder result,  ConditionSubset conditionSubset, int depth)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = SubsetByKey(subsetKey);
                result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + "[" + subExpression.Expression + "]");

                if (subExpression.SubsetKeys.Count > 0)
                {
                    result.AppendLine("  (");
                    BuildFullVirtualExpression(ref result, subExpression, depth + 1);
                    result.AppendLine("  )");
                }
            }

            foreach (var condition in conditionSubset.Conditions)
            {
                //Print something
            }
        }

        #endregion

    }
}
