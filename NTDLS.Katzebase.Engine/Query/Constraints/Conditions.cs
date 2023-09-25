using Katzebase.Engine.Library;
using Katzebase.Engine.Query.Tokenizers;
using Katzebase;
using Katzebase.Exceptions;
using Katzebase.Types;
using System.Text;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Query.Constraints
{
    internal class Conditions
    {
        private string _lastLetter = string.Empty;

        public List<ConditionSubset> Subsets = new();

        /// Every condition instance starts with a single root node that all others poaint back to given some lineage. This is the name of the root node.
        public string RootSubsetKey { get; private set; } = string.Empty;

        public string HighLevelExpressionTree { get; private set; } = string.Empty;

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

        public string? Hash { get; private set; }

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

        public static Conditions Create(string conditionsText, KbInsensitiveDictionary<string> literalStrings, string leftHandAlias = "")
        {
            var conditions = new Conditions();
            conditionsText = conditionsText.ToLowerInvariant();
            conditions.Parse(conditionsText, literalStrings, leftHandAlias);

            conditions.Hash = conditions.BuildConditionHash();

            return conditions;
        }

        private void Parse(string conditionsText, KbInsensitiveDictionary<string> literalStrings, string leftHandAlias)
        {
            //We parse by parentheses so wrap the expression in them if it is not already.
            if (conditionsText.StartsWith('(') == false || conditionsText.StartsWith(')') == false)
            {
                conditionsText = $"({conditionsText})";
            }

            //Push the conditions one group deeper since we want to process all expreessions as groups and leave no chace for conditions at the root level.
            conditionsText = $"({conditionsText})";

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
                        string parenTrimmedSubsetText = subsetText.Substring(1, subsetText.Length - 2).Trim();
                        var subset = new ConditionSubset(subsetVariable, parenTrimmedSubsetText);
                        AddSubset(literalStrings, subset, leftHandAlias);
                        conditionsText = conditionsText.Replace(subsetText, VariableToKey(subsetVariable));
                    }
                }
                else
                {
                    break;
                }
            }

            RootSubsetKey = conditionsText.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            RemoveVariableMarkers();

            //Mark the root subset as such.
            Subsets.Where(o => o.SubsetKey == RootSubsetKey).Single().IsRoot = true;

            HighLevelExpressionTree = BuildHighlevelExpressionTree();

            KbUtility.Assert(Root.Conditions.Any(), "The root expression cannot contain conditions.");
        }

        public string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
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
                RootSubsetKey = this.RootSubsetKey,
                HighLevelExpressionTree = this.HighLevelExpressionTree
            };

            clone.AllFields.AddRange(this.AllFields);

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

            clone.Hash = clone.BuildConditionHash();

            return clone;
        }

        public void AddSubset(KbInsensitiveDictionary<string> literalStrings, ConditionSubset subset, string leftHandAlias)
        {
            var logicalConnector = LogicalConnector.None;

            var conditionTokenizer = new ConditionTokenizer(subset.Expression);

            while (true)
            {
                int startPosition = conditionTokenizer.Position;

                string token = conditionTokenizer.GetNextToken().ToLower();

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
                    token = conditionTokenizer.GetNextToken().ToLower();
                    if (token == "not")
                    {
                        token += " " + conditionTokenizer.GetNextToken().ToLower();
                    }

                    var logicalQualifier = ConditionTokenizer.ParseLogicalQualifier(token);

                    //Righthand value:
                    string right = conditionTokenizer.GetNextToken().ToLower();

                    if (literalStrings.ContainsKey(right))
                    {
                        right = literalStrings[right].ToLowerInvariant();
                    }

                    int endPosition = conditionTokenizer.Position;

                    if (logicalQualifier == LogicalQualifier.Between || logicalQualifier == LogicalQualifier.NotBetween)
                    {
                        string and = conditionTokenizer.GetNextToken().ToLower();
                        if (and != "and")
                        {
                            throw new KbParserException($"Invalid token, Found [{and}] expected [and].");
                        }

                        var rightRange = conditionTokenizer.GetNextToken().ToLower();

                        right = $"{right}:{rightRange}";

                        endPosition = conditionTokenizer.Position;
                    }

                    if (right.StartsWith($"{leftHandAlias}."))
                    {
                        (left, right) = (right, left); //Swap the left and right.
                    }

                    var condition = new Condition(subset.SubsetKey, conditionKey, logicalConnector, left, logicalQualifier, right);

                    subset.Expression = subset.Expression.Remove(startPosition, endPosition - startPosition);
                    subset.Expression = subset.Expression.Insert(startPosition, VariableToKey(conditionKey) + " ");

                    conditionTokenizer.SetText(subset.Expression, 0);

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

        public List<PrefixedField> AllFields { get; private set; } = new();

        /// <summary>
        /// This function is used to build a logical expression at the subset level, it also demonstrates how we process the recursive logic.
        /// </summary>
        /// <returns></returns>
        public string BuildHighlevelExpressionTree()
        {
            var expression = new StringBuilder($"({Root.Expression})");

            foreach (var subsetKey in Root.SubsetKeys)
            {
                var subExpression = SubsetByKey(subsetKey);
                expression.Replace(subsetKey, $"({subExpression.Expression})");
                BuildHighlevelExpressionTree(ref expression, subExpression);
            }

            return expression.ToString();
        }

        public void BuildHighlevelExpressionTree(ref StringBuilder expression, ConditionSubset conditionSubset)
        {
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = SubsetByKey(subsetKey);
                expression.Replace(subsetKey, $"({subExpression.Expression})");
                BuildHighlevelExpressionTree(ref expression, subExpression);
            }
        }

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

                if (subset.Conditions.Count > 0)
                {
                    result.Append('(');
                    foreach (var condition in subset.Conditions)
                    {
                        if (condition.Left?.IsConstant == false)
                        {
                            AllFields.Add(new PrefixedField(condition.Left.Prefix, condition.Left?.Value ?? ""));
                        }
                        if (condition.Right?.IsConstant == false)
                        {
                            AllFields.Add(new PrefixedField(condition.Right.Prefix, condition.Right?.Value ?? ""));
                        }
                        result.Append($"{condition.ConditionKey}: {condition.Left} {condition.LogicalQualifier}");
                    }
                    result.Append(')');
                }

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
                    if (condition.Left?.IsConstant == false)
                    {
                        AllFields.Add(new PrefixedField(condition.Left.Prefix, condition.Left?.Value ?? ""));
                    }
                    if (condition.Right?.IsConstant == false)
                    {
                        AllFields.Add(new PrefixedField(condition.Right.Prefix, condition.Right?.Value ?? ""));
                    }
                    result.Append($"{condition.ConditionKey}: {condition.Left} {condition.LogicalQualifier}");
                }
                result.Append(')');
            }
        }
    }
}
