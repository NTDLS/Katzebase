using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Query.Tokenizers;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Query.Constraints
{
    internal class Conditions
    {
        public const string ExpressionMarker = "e";
        public const string ConditionMarker = "c";

        private int _lastExpression = 0;
        private int _lastCondition = 0;

        public int ExpressionNumber { get => _lastExpression; }
        public int ConditionNumber { get => _lastCondition; }

        public List<SubCondition> SubConditions { get; private set; } = new();

        /// Every condition instance starts with a single root node that all others point back to given some lineage.
        /// This is the name of the root node.
        public string RootKey { get; private set; } = string.Empty;

        /// <summary>
        /// This contains the representative mathematical expression for the condition tree.
        /// It is built at compile-time by a call to CollapseToExpression().
        /// </summary>
        public string Expression { get; private set; } = string.Empty;

        /// <summary>
        /// Every condition instance starts with a single root node that all others point back to given some lineage.
        /// This is the root node.
        /// </summary>
        private SubCondition? _root;
        public SubCondition Root
        {
            get
            {
                _root ??= SubConditions.Single(o => o.IsRoot);
                return _root;
            }
        }

        public string? Hash { get; private set; }

        public IEnumerable<SubCondition> NonRootSubConditions
            => SubConditions.Where(o => !o.IsRoot);

        private static string VariableToKey(string str)
        {
            return $"$:{str}$";
        }

        private string NextExpressionPlaceholder()
        {
            return $"{ExpressionMarker}_{_lastExpression++}";
        }

        private string NextConditionPlaceholder()
        {
            return $"{ConditionMarker}_{_lastCondition++}";
        }

        public static Conditions Create(string conditionsText, KbInsensitiveDictionary<string> literalStrings, string leftHandAliasOfJoin = "")
        {
            var conditions = new Conditions();
            conditionsText = conditionsText.ToLowerInvariant();
            conditions.Parse(conditionsText, literalStrings, leftHandAliasOfJoin);

            conditions.Hash = conditions.BuildConditionHash();

            return conditions;
        }

        private void Parse(string conditionsText, KbInsensitiveDictionary<string> literalStrings, string leftHandAliasOfJoin)
        {
            //We parse by parentheses so wrap the condition in them if it is not already.
            if (conditionsText.StartsWith('(') == false || conditionsText.StartsWith(')') == false)
            {
                conditionsText = $"({conditionsText})";
            }

            //Push the conditions one group deeper since we want to process all
            //  conditions as groups and leave no chance for conditions at the root level.
            conditionsText = $"({conditionsText})";

            while (true)
            {
                int startPos = conditionsText.LastIndexOf('(');
                if (startPos >= 0)
                {
                    int endPos = conditionsText.IndexOf(')', startPos);
                    if (endPos > startPos)
                    {
                        string expressionPlaceholder = NextExpressionPlaceholder();
                        string subConditionText = conditionsText.Substring(startPos, endPos - startPos + 1).Trim();
                        string parenTrimmedSubConditionText = subConditionText.Substring(1, subConditionText.Length - 2).Trim();

                        LogManager.Trace(parenTrimmedSubConditionText);

                        var subCondition = new SubCondition(expressionPlaceholder, parenTrimmedSubConditionText);
                        AddSubCondition(literalStrings, subCondition, leftHandAliasOfJoin);
                        conditionsText = conditionsText.Replace(subConditionText, VariableToKey(expressionPlaceholder));

                        LogManager.Trace(conditionsText);
                    }
                }
                else
                {
                    break;
                }
            }

            RootKey = conditionsText.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            RemoveVariableMarkers();

            //Mark the root SubCondition as such.
            SubConditions.Single(o => o.Key == RootKey).IsRoot = true;

            Expression = CollapseToExpression();

            LogManager.Trace(Expression);

            if (Root.Conditions.Any())
            {
                throw new Exception("The root condition cannot contain conditions.");
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
            RootKey = RemoveVariableMarker(RootKey);

            foreach (var subCondition in SubConditions)
            {
                subCondition.Expression = RemoveVariableMarker(subCondition.Expression);
            }
        }

        private static string RemoveVariableMarker(string str)
        {
            while (true)
            {
                int pos = str.IndexOf("$:"); //SubCondition-Key.
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

        /// <summary>
        /// Gets a sub-condition with the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public SubCondition SubConditionFromKey(string key)
        {
            return SubConditions.First(o => o.Key == key);
        }

        public Conditions Clone()
        {
            var clone = new Conditions()
            {
                RootKey = RootKey,
                Expression = Expression
            };

            clone.AllFields.AddRange(AllFields);

            foreach (var subCondition in SubConditions)
            {
                var subConditionClone = new SubCondition(subCondition.Key, subCondition.Expression)
                {
                    IsRoot = subCondition.IsRoot,
                };

                foreach (var condition in subCondition.Conditions)
                {
                    subConditionClone.Conditions.Add(condition.Clone());
                }
                subConditionClone.ConditionKeys.UnionWith(subCondition.ConditionKeys);
                subConditionClone.Keys.UnionWith(subCondition.Keys);

                clone.SubConditions.Add(subConditionClone);
            }

            clone.Hash = clone.BuildConditionHash();

            return clone;
        }

        public void AddSubCondition(KbInsensitiveDictionary<string> literalStrings, SubCondition subCondition, string leftHandAliasOfJoin)
        {
            var logicalConnector = LogicalConnector.None;

            var conditionTokenizer = new ConditionTokenizer(subCondition.Expression);

            while (true)
            {
                int startPosition = conditionTokenizer.Position;

                string token = conditionTokenizer.GetNextToken().ToLowerInvariant();

                if (token == string.Empty)
                {
                    break; //Done.
                }
                else if (token.StartsWith('$')
                    && token.EndsWith('$')
                    && (token.StartsWith($"$:{ConditionMarker}") || token.StartsWith($"$:{ExpressionMarker}"))
                    )
                {
                    if (token.StartsWith($"$:{ConditionMarker}"))
                    {
                        subCondition.ConditionKeys.Add(token.Substring(2, token.Length - 3));
                    }
                    else if (token.StartsWith($"$:{ExpressionMarker}"))
                    {
                        subCondition.Keys.Add(token.Substring(2, token.Length - 3));
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
                    string conditionPlaceholder = NextConditionPlaceholder();

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

                    if (literalStrings.TryGetValue(left, out string? leftLiteral))
                    {
                        left = leftLiteral.ToLowerInvariant();
                    }
                    if (literalStrings.TryGetValue(right, out string? rightLiteral))
                    {
                        right = rightLiteral.ToLowerInvariant();
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

                    var condition = new Condition(subCondition.Key, conditionPlaceholder, logicalConnector, left, logicalQualifier, right);


                    if (right.StartsWith($"{leftHandAliasOfJoin}."))
                    {
                        //For joins, keep the left and right values on the side that I prefer.
                        LogManager.Trace($"Conditions.AddSubCondition: Inverting schema join condition.");
                        condition.Invert();
                    }

                    if (condition.Left.IsConstant && !condition.Right.IsConstant)
                    {
                        LogManager.Trace($"Conditions.AddSubCondition: Inverting constant condition.");
                        condition.Invert();
                    }

                    LogManager.Trace("{condition.Left} {condition.LogicalQualifier} {condition.Right}");

                    subCondition.Expression = subCondition.Expression.Remove(startPosition, endPosition - startPosition);
                    subCondition.Expression = subCondition.Expression.Insert(startPosition, VariableToKey(conditionPlaceholder) + " ");

                    conditionTokenizer.SetText(subCondition.Expression, 0);

                    subCondition.Conditions.Add(condition);
                    logicalConnector = LogicalConnector.None;
                }
            }

            subCondition.Expression = subCondition.Expression.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            SubConditions.Add(subCondition);
        }

        #region Debug.

        public static string FriendlyPlaceholder(string val) => val.ToUpper()
            .Replace($"{ConditionMarker}_".ToUpper(), "Cond")
            .Replace($"{ExpressionMarker}_".ToUpper(), "Expr");

        private static string Pad(int indentation)
            => "".PadLeft(indentation * 2, ' ');

        /// <summary>
        /// This function makes a (somewhat) user readable expression tree, used for debugging and explanations.
        /// It also demonstrates how we process the recursive condition logic.
        /// </summary>
        public string ExplainConditionTree(int indentation = 0)
        {
            var result = new StringBuilder();

            if (Root.Keys.Count > 0)
            {
                //The root condition is just a pointer to a child condition, so get the "root" child condition.
                var rootCondition = SubConditionFromKey(Root.Key);
                ExplainConditionTree(ref result, rootCondition, indentation);
            }

            return result.ToString();
        }

        /// <summary>
        /// This function makes a (somewhat) user readable expression tree, used for debugging and explanations.
        /// It also demonstrates how we process the recursive condition logic.
        /// Called by parent ExplainConditionTree()
        /// </summary>
        private void ExplainConditionTree(ref StringBuilder result, SubCondition givenSubCondition, int indentation)
        {
            foreach (var subConditionKey in givenSubCondition.Keys)
            {
                var subCondition = SubConditionFromKey(subConditionKey);

                var indexName = subCondition.IndexSelection?.PhysicalIndex?.Name;

                result.AppendLine(Pad(indentation + 1)
                    + $"{FriendlyPlaceholder(subCondition.Key)} is ({FriendlyPlaceholder(subCondition.Expression)})");

                result.AppendLine(Pad(indentation + 1) + "(");

                if (subCondition.Conditions.Count > 0)
                {
                    foreach (var condition in subCondition.Conditions)
                    {
                        result.AppendLine(Pad(indentation + 2)
                            + $"{FriendlyPlaceholder(condition.ConditionKey)} is ({condition.Left} {condition.LogicalQualifier} {condition.Right})");
                    }
                }

                if (subCondition.Keys.Count > 0)
                {
                    result.AppendLine(Pad(indentation + 2) + "(");
                    ExplainConditionTree(ref result, subCondition, indentation + 2);
                    result.AppendLine(Pad(indentation + 2) + ")");
                }

                result.AppendLine(Pad(indentation + 1) + ")");
            }
        }

        #endregion

        public List<PrefixedField> AllFields { get; private set; } = new();

        /// <summary>
        /// Builds mathematical expression that represents the entire condition tree.
        /// It also demonstrates how we process the recursive condition logic.
        /// </summary>
        public string CollapseToExpression()
        {
            var result = new StringBuilder($"({Root.Expression})");

            foreach (var key in Root.Keys)
            {
                var condition = SubConditionFromKey(key);
                result.Replace(key, $"({condition.Expression})");
                CollapseToExpression(ref result, condition);
            }

            return result.ToString();
        }

        public void CollapseToExpression(ref StringBuilder result, SubCondition givenSubCondition)
        {
            foreach (var key in givenSubCondition.Keys)
            {
                var condition = SubConditionFromKey(key);
                result.Replace(key, $"({condition.Expression})");
                CollapseToExpression(ref result, condition);
            }
        }

        public string BuildConditionHash()
        {
            var result = new StringBuilder();
            result.Append($"[{RootKey}]");

            if (Root?.Keys?.Count > 0)
            {
                result.Append('(');

                foreach (var subConditionKey in Root.Keys)
                {
                    var subCondition = SubConditionFromKey(subConditionKey);
                    result.Append($"[{subCondition.Expression}]");
                    BuildConditionHash(ref result, subCondition, 1);
                }

                result.Append(')');
            }

            return Library.Helpers.ComputeSHA256(result.ToString());
        }

        private void BuildConditionHash(ref StringBuilder result, SubCondition givenSubCondition, int depth)
        {
            //If we have SubConditions, then we need to satisfy those in order to complete the equation.
            foreach (var subConditionKey in givenSubCondition.Keys)
            {
                var subCondition = SubConditionFromKey(subConditionKey);
                result.Append($"[{subCondition.Expression}]");

                if (subCondition.Conditions.Count > 0)
                {
                    result.Append('(');
                    foreach (var condition in subCondition.Conditions)
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

                if (subCondition.Keys.Count > 0)
                {
                    result.Append('(');
                    BuildConditionHash(ref result, subCondition, depth + 1);
                    result.Append(')');
                }
            }

            if (givenSubCondition.Conditions.Count > 0)
            {
                result.Append('(');
                foreach (var condition in givenSubCondition.Conditions)
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
