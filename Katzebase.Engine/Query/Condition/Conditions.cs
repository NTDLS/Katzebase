using Katzebase.Library;
using System.Text;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class Conditions
    {

        private string _lastLetter = string.Empty;

        public List<ConditionSubset> Subsets = new();

        /// Every condition instance starts with a single root node that all others poaint back to given some lineage. This is the name of the root node.
        public string RootSubsetKey { get; set; } = string.Empty;

        /// <summary>
        /// Every condition instance starts with a single root node that all others poaint back to given some lineage. This is the root node.
        /// </summary>
        private ConditionSubset? _root;
        public ConditionSubset Root
        {
            get
            {
                _root ??= Subsets.Where(o => o.IsRoot).Single();
                return _root;
            }
        }

        private string? _hash = null;

        public string Hash
        {
            get
            {
                _hash ??= BuildConditionHash();
                return _hash;
            }
        }

        public IEnumerable<ConditionSubset> NonRootSubsets => Subsets.Where(o => !o.IsRoot);

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
            //We parse by parentheses so wrap the expression in them if it is not already.
            if (conditionsText.StartsWith('(') == false || conditionsText.StartsWith(')') == false)
            {
                if (conditionsText.Contains('(') == false && conditionsText.Contains(')') == false)
                {
                    //If we have no sub-expressions at all, push the conditions one group deeper since we want to process all expreessions as groups.
                    conditionsText = $"({conditionsText})";
                }

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

            this.RootSubsetKey = conditionsText.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            RemoveVariableMarkers();

            //Mark the root subset as such.
            Subsets.Where(o => o.SubsetKey == RootSubsetKey).Single().IsRoot = true;

            Utility.Assert(Root.Conditions.Any(), "The root expression cannot contain conditions.");
        }

        private void RemoveVariableMarkers()
        {
            RootSubsetKey = RemoveVariableMarker(RootSubsetKey);

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
                RootSubsetKey = this.RootSubsetKey
            };

            foreach (var subset in Subsets)
            {
                var subsetClone = new ConditionSubset(subset.SubsetKey, subset.Expression)
                {
                    IsRoot = subset.IsRoot,
                };

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
            result.AppendLine($"[{RootSubsetKey}]");

            if (Root.SubsetKeys.Count > 0)
            {
                result.AppendLine("(");

                foreach (var subsetKey in Root.SubsetKeys)
                {
                    var subset = SubsetByKey(subsetKey);
                    result.AppendLine($"  [{subset.Expression}]");
                    BuildFullVirtualExpression(ref result, subset, 1);
                }

                result.AppendLine(")");
            }

            return result.ToString();
        }

        private void BuildFullVirtualExpression(ref StringBuilder result, ConditionSubset conditionSubset, int depth)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subset = SubsetByKey(subsetKey);
                result.AppendLine("".PadLeft((depth) * 2, ' ') + $"[{subset.Expression}]");

                if (subset.SubsetKeys.Count > 0)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + "(");
                    BuildFullVirtualExpression(ref result, subset, depth + 1);
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + ")");
                }
            }

            if (conditionSubset.Conditions.Count > 0)
            {
                result.AppendLine("".PadLeft((depth + 1) * 1, ' ') + "(");
                foreach (var condition in conditionSubset.Conditions)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + $"{condition.ConditionKey}: {condition.Left} {condition.LogicalQualifier}");
                }
                result.AppendLine("".PadLeft((depth + 1) * 1, ' ') + ")");
            }
        }

        #endregion

        public string BuildConditionHash()
        {
            var result = new StringBuilder();
            result.Append($"[{RootSubsetKey}]");

            if (Root.SubsetKeys.Count > 0)
            {
                result.Append('(');

                foreach (var subsetKey in Root.SubsetKeys)
                {
                    var subset = SubsetByKey(subsetKey);
                    result.Append($"[{subset.Expression}]");
                    BuildConditionHash(ref result, subset, 1);
                }

                result.Append(')');
            }

            return Helpers.ComputeSHA256(result.ToString());
        }

        private void BuildConditionHash(ref StringBuilder result, ConditionSubset conditionSubset, int depth)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subset = SubsetByKey(subsetKey);
                result.Append($"[{subset.Expression}]");

                if (subset.SubsetKeys.Count > 0)
                {
                    result.Append('(');
                    BuildConditionHash(ref result, subset, depth + 1);
                    result.Append(')');
                }
            }

            if (conditionSubset.Conditions.Count > 0)
            {
                result.Append('(');
                foreach (var condition in conditionSubset.Conditions)
                {
                    result.Append($"{condition.ConditionKey}: {condition.Left} {condition.LogicalQualifier}");
                }
                result.Append(')');
            }
        }
    }
}
