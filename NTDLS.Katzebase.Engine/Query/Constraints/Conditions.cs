using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Query.Tokenizers;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Query.Constraints
{
    internal class Conditions
    {
        private string _lastLetter = string.Empty;

        public List<ConditionSubExpression> SubExpressions { get; private set; } = new();

        /// Every condition instance starts with a single root node that all others point back to given some lineage.
        /// This is the name of the root node.
        public string RootSubExpressionKey { get; private set; } = string.Empty;

        public string HighLevelExpressionTree { get; private set; } = string.Empty;

        /// <summary>
        /// Every condition instance starts with a single root node that all others point back to given some lineage.
        /// This is the root node.
        /// </summary>
        private ConditionSubExpression? _root;
        public ConditionSubExpression Root
        {
            get
            {
                _root ??= SubExpressions.Single(o => o.IsRoot);
                return _root;
            }
        }

        public string? Hash { get; private set; }

        public IEnumerable<ConditionSubExpression> NonRootSubExpressions => SubExpressions.Where(o => !o.IsRoot);

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

            //Push the conditions one group deeper since we want to process all
            //  expressions as groups and leave no chance for conditions at the root level.
            conditionsText = $"({conditionsText})";

            while (true)
            {
                int startPos = conditionsText.LastIndexOf('(');
                if (startPos >= 0)
                {
                    int endPos = conditionsText.IndexOf(')', startPos);
                    if (endPos > startPos)
                    {
                        string subExpressionVariable = $"s_{GetNextVariableLetter()}"; //S=SubExpression-Key
                        string subExpressionText = conditionsText.Substring(startPos, endPos - startPos + 1).Trim();
                        string parenTrimmedSubExpressionText = subExpressionText.Substring(1, subExpressionText.Length - 2).Trim();
                        var subExpression = new ConditionSubExpression(subExpressionVariable, parenTrimmedSubExpressionText);
                        AddSubExpression(literalStrings, subExpression, leftHandAlias);
                        conditionsText = conditionsText.Replace(subExpressionText, VariableToKey(subExpressionVariable));
                    }
                }
                else
                {
                    break;
                }
            }

            RootSubExpressionKey = conditionsText.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            RemoveVariableMarkers();

            //Mark the root SubExpression as such.
            SubExpressions.Single(o => o.SubExpressionKey == RootSubExpressionKey).IsRoot = true;

            HighLevelExpressionTree = BuildHighLevelExpressionTree();

            if (Root.Expressions.Any())
            {
                throw new Exception("The root expression cannot contain conditions.");
            }
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
            RootSubExpressionKey = RemoveVariableMarker(RootSubExpressionKey);

            foreach (var subExpression in SubExpressions)
            {
                subExpression.Expression = RemoveVariableMarker(subExpression.Expression);
            }
        }

        private static string RemoveVariableMarker(string str)
        {
            while (true)
            {
                int pos = str.IndexOf("$:"); //SubExpression-Key.
                if (pos >= 0)
                {
                    int epos = str.IndexOf("$", pos + 1);
                    if (epos > pos)
                    {
                        var tok = str.Substring(pos, epos - pos + 1);
                        var key = str.Substring(pos + 2, (epos - pos) - 2);
                        str = str.Replace(tok, key);
                    }
                    else break;
                }
                else break;
            }

            return str;
        }

        public ConditionSubExpression SubExpressionByKey(string key)
        {
            return SubExpressions.First(o => o.SubExpressionKey == key);
        }

        public Conditions Clone()
        {
            var clone = new Conditions()
            {
                RootSubExpressionKey = RootSubExpressionKey,
                HighLevelExpressionTree = HighLevelExpressionTree
            };

            clone.AllFields.AddRange(AllFields);

            foreach (var subExpression in SubExpressions)
            {
                var subExpressionClone = new ConditionSubExpression(subExpression.SubExpressionKey, subExpression.Expression)
                {
                    IsRoot = subExpression.IsRoot,
                };

                foreach (var condition in subExpression.Expressions)
                {
                    subExpressionClone.Expressions.Add(condition.Clone());
                }
                subExpressionClone.ConditionKeys.UnionWith(subExpression.ConditionKeys);
                subExpressionClone.SubExpressionKeys.UnionWith(subExpression.SubExpressionKeys);

                clone.SubExpressions.Add(subExpressionClone);
            }

            clone.Hash = clone.BuildConditionHash();

            return clone;
        }

        public void AddSubExpression(KbInsensitiveDictionary<string> literalStrings, ConditionSubExpression subExpression, string leftHandAlias)
        {
            var logicalConnector = LogicalConnector.None;

            var conditionTokenizer = new ConditionTokenizer(subExpression.Expression);

            while (true)
            {
                int startPosition = conditionTokenizer.Position;

                string token = conditionTokenizer.GetNextToken().ToLowerInvariant();

                if (token == string.Empty)
                {
                    break; //Done.
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    if (token.StartsWith("$:c"))
                    {
                        subExpression.ConditionKeys.Add(token.Substring(2, token.Length - 3));
                    }
                    else if (token.StartsWith("$:s"))
                    {
                        subExpression.SubExpressionKeys.Add(token.Substring(2, token.Length - 3));
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
                    token = conditionTokenizer.GetNextToken().ToLowerInvariant();
                    if (token == "not")
                    {
                        token += " " + conditionTokenizer.GetNextToken().ToLowerInvariant();
                    }

                    var logicalQualifier = ConditionTokenizer.ParseLogicalQualifier(token);

                    //Righthand value:
                    string right = conditionTokenizer.GetNextToken().ToLowerInvariant();

                    if (literalStrings.ContainsKey(right))
                    {
                        right = literalStrings[right].ToLowerInvariant();
                    }

                    int endPosition = conditionTokenizer.Position;

                    if (logicalQualifier == LogicalQualifier.Between || logicalQualifier == LogicalQualifier.NotBetween)
                    {
                        string and = conditionTokenizer.GetNextToken().ToLowerInvariant();
                        if (and != "and")
                        {
                            throw new KbParserException($"Invalid token, Found [{and}] expected [and].");
                        }

                        var rightRange = conditionTokenizer.GetNextToken().ToLowerInvariant();

                        right = $"{right}:{rightRange}";

                        endPosition = conditionTokenizer.Position;
                    }

                    if (right.StartsWith($"{leftHandAlias}."))
                    {
                        (left, right) = (right, left); //Swap the left and right.
                    }

                    var expression = new ConditionExpression(subExpression.SubExpressionKey, conditionKey, logicalConnector, left, logicalQualifier, right);

                    subExpression.Expression = subExpression.Expression.Remove(startPosition, endPosition - startPosition);
                    subExpression.Expression = subExpression.Expression.Insert(startPosition, VariableToKey(conditionKey) + " ");

                    conditionTokenizer.SetText(subExpression.Expression, 0);

                    subExpression.Expressions.Add(expression);
                    logicalConnector = LogicalConnector.None;
                }
            }

            subExpression.Expression = subExpression.Expression.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            SubExpressions.Add(subExpression);
        }

        #region Debug.

        public string BuildFullVirtualExpression()
        {
            var result = new StringBuilder();
            result.AppendLine($"[{RootSubExpressionKey}]");

            if (Root.SubExpressionKeys.Count > 0)
            {
                result.AppendLine("(");

                foreach (var subExpressionKey in Root.SubExpressionKeys)
                {
                    var subExpression = SubExpressionByKey(subExpressionKey);
                    result.AppendLine($"  [{subExpression.Expression}]");
                    BuildFullVirtualExpression(ref result, subExpression, 1);
                }

                result.AppendLine(")");
            }

            return result.ToString();
        }

        private void BuildFullVirtualExpression(ref StringBuilder result, ConditionSubExpression conditionSubExpression, int depth)
        {
            //If we have SubExpressions, then we need to satisfy those in order to complete the equation.
            foreach (var subExpressionKey in conditionSubExpression.SubExpressionKeys)
            {
                var subExpression = SubExpressionByKey(subExpressionKey);
                result.AppendLine("".PadLeft((depth) * 2, ' ') + $"[{subExpression.Expression}]");

                if (subExpression.SubExpressionKeys.Count > 0)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + "(");
                    BuildFullVirtualExpression(ref result, subExpression, depth + 1);
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + ")");
                }
            }

            if (conditionSubExpression.Expressions.Count > 0)
            {
                result.AppendLine("".PadLeft((depth + 1) * 1, ' ') + "(");
                foreach (var condition in conditionSubExpression.Expressions)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + $"{condition.ConditionKey}: {condition.Left} {condition.LogicalQualifier}");
                }
                result.AppendLine("".PadLeft((depth + 1) * 1, ' ') + ")");
            }
        }

        #endregion

        public List<PrefixedField> AllFields { get; private set; } = new();

        /// <summary>
        /// This function is used to build a logical expression at the SubExpression
        ///     level, it also demonstrates how we process the recursive logic.
        /// </summary>
        /// <returns></returns>
        public string BuildHighLevelExpressionTree()
        {
            var expression = new StringBuilder($"({Root.Expression})");

            foreach (var subExpressionKey in Root.SubExpressionKeys)
            {
                var subExpression = SubExpressionByKey(subExpressionKey);
                expression.Replace(subExpressionKey, $"({subExpression.Expression})");
                BuildHighLevelExpressionTree(ref expression, subExpression);
            }

            return expression.ToString();
        }

        public void BuildHighLevelExpressionTree(ref StringBuilder expression, ConditionSubExpression conditionSubExpression)
        {
            foreach (var subExpressionKey in conditionSubExpression.SubExpressionKeys)
            {
                var subExpression = SubExpressionByKey(subExpressionKey);
                expression.Replace(subExpressionKey, $"({subExpression.Expression})");
                BuildHighLevelExpressionTree(ref expression, subExpression);
            }
        }

        public string BuildConditionHash()
        {
            var result = new StringBuilder();
            result.Append($"[{RootSubExpressionKey}]");

            if (Root.SubExpressionKeys.Count > 0)
            {
                result.Append('(');

                foreach (var subExpressionKey in Root.SubExpressionKeys)
                {
                    var subExpression = SubExpressionByKey(subExpressionKey);
                    result.Append($"[{subExpression.Expression}]");
                    BuildConditionHash(ref result, subExpression, 1);
                }

                result.Append(')');
            }

            return Library.Helpers.ComputeSHA256(result.ToString());
        }

        private void BuildConditionHash(ref StringBuilder result, ConditionSubExpression conditionSubExpression, int depth)
        {
            //If we have SubExpressions, then we need to satisfy those in order to complete the equation.
            foreach (var subExpressionKey in conditionSubExpression.SubExpressionKeys)
            {
                var subExpression = SubExpressionByKey(subExpressionKey);
                result.Append($"[{subExpression.Expression}]");

                if (subExpression.Expressions.Count > 0)
                {
                    result.Append('(');
                    foreach (var condition in subExpression.Expressions)
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

                if (subExpression.SubExpressionKeys.Count > 0)
                {
                    result.Append('(');
                    BuildConditionHash(ref result, subExpression, depth + 1);
                    result.Append(')');
                }
            }

            if (conditionSubExpression.Expressions.Count > 0)
            {
                result.Append('(');
                foreach (var condition in conditionSubExpression.Expressions)
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
