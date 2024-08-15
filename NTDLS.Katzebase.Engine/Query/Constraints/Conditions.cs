using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Query.Tokenizers;
using NTDLS.Katzebase.Shared;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Query.Constraints
{
    internal class Conditions
    {
        private SubCondition? _root;

        public const string TempKeyMarker = "t";
        private int _lastTempKey = 0;

        public const string ExpressionKeyMarker = "e";
        public const string ConditionKeyMarker = "c";

        private int _lastExpressionKey = 0;
        private int _lastConditionKey = 0;

        public string? Hash { get; private set; }

        public List<SubCondition> SubConditions { get; private set; } = new();

        /// <summary>
        /// List of all fields referenced by the conditions tree.
        /// </summary>
        public List<PrefixedField> AllFields { get; private set; } = new();

        /// <summary>
        /// The name of the SubConditions key which is the root of the expression tree.
        /// </summary>
        public string RootKey { get; private set; } = string.Empty;

        /// <summary>
        /// This contains the representative mathematical expression for the condition tree.
        /// It is built at compile-time by a call to CollapseToExpression().
        /// </summary>
        public string Expression { get; private set; } = string.Empty;

        /// <summary>
        /// Every condition instance starts with a single root node that all others 
        ///     point back to (given some lineage). This is it, the root node.
        /// </summary>
        public SubCondition Root
            => _root.EnsureNotNull();

        public IEnumerable<SubCondition> NonRootSubConditions
            => SubConditions.Where(o => !o.IsRoot);

        /// <summary>
        /// Gets a sub-condition with the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public SubCondition SubConditionFromExpressionKey(string key)
            => SubConditions.First(o => o.Key == key);

        public static Conditions Create(string conditionsText,
            KbInsensitiveDictionary<string> stringLiterals,
            KbInsensitiveDictionary<string> numericLiterals,
            string leftHandAliasOfJoin = "")
        {
            var conditions = new Conditions();

            conditions.Parse(conditionsText.ToLowerInvariant(), stringLiterals, numericLiterals, leftHandAliasOfJoin);

            return conditions;
        }

        #region Parser.

        private void Parse(string givenConditionText, KbInsensitiveDictionary<string> stringLiterals,
            KbInsensitiveDictionary<string> numericLiterals, string leftHandAliasOfJoin)
        {
            //We parse by parentheses so wrap the condition in them if it is not already.
            if (givenConditionText.StartsWith('(') == false || givenConditionText.StartsWith(')') == false)
            {
                givenConditionText = $"({givenConditionText})";
            }

            //Push the conditions one group deeper since we want to process all
            //  conditions as groups and leave no chance for conditions at the root level.
            givenConditionText = $"({givenConditionText})";

            int startPos = 0, endPos = 0;

            var orLiterals = new Dictionary<string, string>();

            //Extract all sub-expressions, check each one of them for OR conditions and then push
            //   all of those OR conditions lower into sub expressions by wrapping them in parentheses.
            while (true)
            {
                if ((startPos = givenConditionText.LastIndexOf('(')) >= 0)
                {
                    if ((endPos = givenConditionText.IndexOf(')', startPos)) > startPos)
                    {
                        string subConditionText = givenConditionText.Substring(startPos, endPos - startPos + 1).Trim();
                        string parenTrimmedSubConditionText = subConditionText.Substring(1, subConditionText.Length - 2).Trim();

                        var orConditions = parenTrimmedSubConditionText.Split(" or ").ToList();
                        if (orConditions.Count > 1)
                        {
                            string orConditionWithConnector = string.Empty;
                            for (int i = 0; i < orConditions.Count; i++)
                            {
                                orConditionWithConnector += $"({orConditions[i]})";
                                if (i < orConditions.Count - 1)
                                {
                                    orConditionWithConnector += " or ";
                                }
                            }

                            //We have to store keys to point to the sub-expressions so we can know when we are done parsing.
                            var key = NextTempKey();
                            orLiterals.Add(key, orConditionWithConnector);

                            givenConditionText = ReplaceRange(givenConditionText, startPos, endPos - startPos + 1, key);
                        }
                        else
                        {
                            //We have to store keys to point to the sub-expressions so we can know when we are done parsing.
                            var key = NextTempKey();
                            orLiterals.Add(key, subConditionText);

                            givenConditionText = ReplaceRange(givenConditionText, startPos, endPos - startPos + 1, key);
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            //TODO: Replace with NTDLS.Helpers.Text when nugets get updated.
            static string ReplaceRange(string original, int startIndex, int length, string replacement)
            {
                // Remove the range of text to be replaced
                string removed = original.Remove(startIndex, length);
                // Insert the replacement string at the start index
                string result = removed.Insert(startIndex, replacement);
                return result;
            }

            //Swap back in all of the temporarily stored sub-expressions from the OR parsing.
            foreach (var kvp in orLiterals.Reverse())
            {
                if (givenConditionText.Contains(kvp.Key))
                {
                    givenConditionText = givenConditionText.Replace(kvp.Key, kvp.Value);
                }
            }

            //Roll through the condition text looking for groups of conditions wrapped in parentheses.
            //Extract each sub-condition text from those parentheses, process them and then replace the
            //  text inside the parentheses with the sub-expression key.
            while (true)
            {
                if ((startPos = givenConditionText.LastIndexOf('(')) >= 0)
                {
                    if ((endPos = givenConditionText.IndexOf(')', startPos)) > startPos)
                    {
                        string subExpressionKey = NextExpressionKey();
                        string subConditionText = givenConditionText.Substring(startPos, endPos - startPos + 1).Trim();
                        string parenTrimmedSubConditionText = subConditionText.Substring(1, subConditionText.Length - 2).Trim();

                        LogicalConnector logicalConnector = LogicalConnector.None;

                        if (startPos > 4)
                        {
                            var logicalConnectorString = givenConditionText.Substring(startPos - 4, 3).Trim();

                            if (logicalConnectorString.Is("or"))
                            {
                                logicalConnector = LogicalConnector.Or;
                            }
                            else if (logicalConnectorString.Is("and"))
                            {
                                logicalConnector = LogicalConnector.And;
                            }
                            else
                            {
                                throw new KbParserException($"Expected [and] or [or].");
                            }
                        }

                        var subCondition = new SubCondition(subExpressionKey, logicalConnector, parenTrimmedSubConditionText);

                        AddSubCondition(stringLiterals, numericLiterals, subCondition, leftHandAliasOfJoin);

                        givenConditionText = ReplaceRange(givenConditionText, startPos, endPos - startPos + 1, VariableToKey(subExpressionKey));
                    }
                }
                else
                {
                    break;
                }
            }

            RootKey = givenConditionText.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            RemoveVariableMarkers();

            _root = SubConditions.Single(o => o.Key == RootKey);
            _root.IsRoot = true;

            Expression = CollapseToExpression();

            Hash = BuildConditionHash();

            LogManager.Trace(Expression);

            if (Root.Conditions.Count != 0)
            {
                throw new Exception("Root condition cannot contain conditions.");
            }
        }

        private static string VariableToKey(string str)
        {
            return $"$:{str}$";
        }
        private string NextTempKey()
        {
            return $"{TempKeyMarker}_{_lastTempKey++}";
        }

        private string NextExpressionKey()
        {
            return $"{ExpressionKeyMarker}_{_lastExpressionKey++}";
        }

        private string NextConditionKey()
        {
            return $"{ConditionKeyMarker}_{_lastConditionKey++}";
        }

        private string ReplaceFirst(string text, string search, string replace)
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

        private void AddSubCondition(KbInsensitiveDictionary<string> stringLiterals,
            KbInsensitiveDictionary<string> numericLiterals, SubCondition subCondition, string leftHandAliasOfJoin)
        {
            var logicalConnector = LogicalConnector.None;

            var tokenizer = new ConditionTokenizer(subCondition.Expression);

            while (true)
            {
                int startPosition = tokenizer.Position;

                string token = tokenizer.GetNext().ToLowerInvariant();

                if (token == string.Empty)
                {
                    break; //Done.
                }
                else if (token.StartsWith("$:") && token.EndsWith('$')
                    && (token.StartsWith($"$:{ConditionKeyMarker}") || token.StartsWith($"$:{ExpressionKeyMarker}"))
                    )
                {
                    if (token.StartsWith($"$:{ConditionKeyMarker}"))
                    {
                        subCondition.ConditionKeys.Add(token.Substring(2, token.Length - 3));
                    }
                    else if (token.StartsWith($"$:{ExpressionKeyMarker}"))
                    {
                        subCondition.ExpressionKeys.Add(token.Substring(2, token.Length - 3));
                    }
                    else
                    {
                        throw new KbParserException($"Invalid token, Found [{token}] expected [ConditionKeyMarker] or [ExpressionKeyMarker].");
                    }

                    continue;
                }
                else if (token.Is("and"))
                {
                    logicalConnector = LogicalConnector.And;
                    continue;
                }
                else if (token.Is("or"))
                {
                    logicalConnector = LogicalConnector.Or;
                    continue;
                }
                else
                {
                    string conditionPlaceholder = NextConditionKey();

                    string left = token;

                    //Logical Qualifier
                    token = tokenizer.GetNext().ToLowerInvariant();
                    if (token.Is("not"))
                    {
                        token += " " + tokenizer.GetNext().ToLowerInvariant();
                    }

                    var logicalQualifier = ConditionTokenizer.ParseLogicalQualifier(token);

                    //Righthand value:
                    string right = tokenizer.GetNext().ToLowerInvariant();

                    if (stringLiterals.TryGetValue(left, out string? leftLiteral))
                    {
                        left = leftLiteral.ToLowerInvariant();
                    }
                    if (stringLiterals.TryGetValue(right, out string? rightLiteral))
                    {
                        right = rightLiteral.ToLowerInvariant();
                    }
                    if (numericLiterals.TryGetValue(left, out string? leftLiteralNumeric))
                    {
                        left = leftLiteralNumeric.ToLowerInvariant();
                    }
                    if (numericLiterals.TryGetValue(right, out string? rightLiteralNumeric))
                    {
                        right = rightLiteralNumeric.ToLowerInvariant();
                    }

                    int endPosition = tokenizer.Position;

                    if (logicalQualifier == LogicalQualifier.Between || logicalQualifier == LogicalQualifier.NotBetween)
                    {
                        string and = tokenizer.GetNext().ToLowerInvariant();
                        if (and.Is("and") == false)
                        {
                            throw new KbParserException($"Invalid token, Found [{and}] expected [and].");
                        }

                        var rightRange = tokenizer.GetNext().ToLowerInvariant();

                        right = $"{right}:{rightRange}";

                        endPosition = tokenizer.Position;
                    }

                    var condition = new Condition(conditionPlaceholder, logicalConnector, left, logicalQualifier, right);

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

                    tokenizer.SetText(subCondition.Expression, 0);

                    if (subCondition.Conditions.Count > 0)
                    {
                        if (condition.LogicalConnector == LogicalConnector.None)
                        {
                            throw new KbParserException($"Expected [and] or [or].");
                        }
                    }

                    subCondition.Conditions.Add(condition);
                    logicalConnector = LogicalConnector.None;
                }
            }

            subCondition.Expression = subCondition.Expression.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            SubConditions.Add(subCondition);
        }

        /// <summary>
        /// Builds mathematical expression that represents the entire condition tree.
        /// It also demonstrates how we process the recursive condition logic.
        /// </summary>
        private string CollapseToExpression()
        {
            var result = new StringBuilder($"({Root.Expression})");

            foreach (var key in Root.ExpressionKeys)
            {
                var condition = SubConditionFromExpressionKey(key);
                result.Replace(key, $"({condition.Expression})");
                CollapseToExpression(ref result, condition);
            }

            return result.ToString();
        }

        private void CollapseToExpression(ref StringBuilder result, SubCondition givenSubCondition)
        {
            foreach (var key in givenSubCondition.ExpressionKeys)
            {
                var condition = SubConditionFromExpressionKey(key);
                result.Replace(key, $"({condition.Expression})");
                CollapseToExpression(ref result, condition);
            }
        }

        private string BuildConditionHash()
        {
            var result = new StringBuilder();
            result.Append($"[{RootKey}]");

            if (Root?.ExpressionKeys?.Count > 0)
            {
                result.Append('(');

                foreach (var expressionKey in Root.ExpressionKeys)
                {
                    var subCondition = SubConditionFromExpressionKey(expressionKey);
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
            foreach (var expressionKey in givenSubCondition.ExpressionKeys)
            {
                var subCondition = SubConditionFromExpressionKey(expressionKey);
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

                if (subCondition.ExpressionKeys.Count > 0)
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

        #endregion

        public Conditions Clone()
        {
            var clone = new Conditions()
            {
                RootKey = RootKey,
                Expression = Expression,
                Hash = Hash
            };

            clone.AllFields.AddRange(AllFields);

            foreach (var subCondition in SubConditions)
            {
                var subConditionClone = new SubCondition(subCondition.Key, subCondition.LogicalConnector, subCondition.Expression);

                if (subCondition.IsRoot)
                {
                    subConditionClone.IsRoot = true;
                    clone._root = subConditionClone;
                };

                foreach (var condition in subCondition.Conditions)
                {
                    subConditionClone.Conditions.Add(condition.Clone());
                }
                subConditionClone.ConditionKeys.UnionWith(subCondition.ConditionKeys);
                subConditionClone.ExpressionKeys.UnionWith(subCondition.ExpressionKeys);

                clone.SubConditions.Add(subConditionClone);
            }

            return clone;
        }

        #region Debug.

        public static string FriendlyPlaceholder(string val) => val.ToUpper()
            .Replace($"{ConditionKeyMarker}_".ToUpper(), "Cond")
            .Replace($"{ExpressionKeyMarker}_".ToUpper(), "Expr");

        private static string Pad(int indentation)
            => "".PadLeft(indentation * 2, ' ');

        /// <summary>
        /// This function makes a (somewhat) user readable expression tree, used for debugging and explanations.
        /// It also demonstrates how we process the recursive condition logic.
        /// </summary>
        public string ExplainConditionTree(int indentation = 0)
        {
            var result = new StringBuilder();

            if (Root.ExpressionKeys.Count > 0)
            {
                //The root condition is just a pointer to a child condition, so get the "root" child condition.
                var rootCondition = SubConditionFromExpressionKey(Root.Key);
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
            foreach (var expressionKey in givenSubCondition.ExpressionKeys)
            {
                var subCondition = SubConditionFromExpressionKey(expressionKey);

                //var indexName = subCondition.IndexSelection?.Index?.Name;

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

                if (subCondition.ExpressionKeys.Count > 0)
                {
                    result.AppendLine(Pad(indentation + 2) + "(");
                    ExplainConditionTree(ref result, subCondition, indentation + 2);
                    result.AppendLine(Pad(indentation + 2) + ")");
                }

                result.AppendLine(Pad(indentation + 1) + ")");
            }
        }

        #endregion
    }
}
