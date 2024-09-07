using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using NTDLS.Katzebase.Shared;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
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

        public QueryBatch QueryBatch { get; private set; }

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

        public Conditions(QueryBatch queryBatch)
        {
            QueryBatch = queryBatch;
        }

        /// <summary>
        /// Gets a sub-condition with the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public SubCondition SubConditionFromExpressionKey(string key)
            => SubConditions.First(o => o.Key == key);

        /// <summary>
        /// Creates a new instance of conditions containing the parsed sub-conditions.
        /// </summary>
        /// <param name="queryBatch"></param>
        /// <param name="conditionsText"></param>
        /// <param name="tokenizer"></param>
        /// <param name="leftHandAliasOfJoin">When parsing a JOIN, this is the schema that we are joining to.</param>
        /// <returns></returns>
        public static Conditions Create(QueryBatch queryBatch, string conditionsText, Tokenizer tokenizer, string leftHandAliasOfJoin = "")
        {
            var conditions = new Conditions(queryBatch);

            conditions.Parse(conditionsText.ToLowerInvariant(), tokenizer, leftHandAliasOfJoin);

            return conditions;
        }

        #region Parser.

        /// <summary>
        /// Flattens the conditions into the SubConditions.
        /// </summary>
        /// <param name="givenConditionText">Query text for the entire condition section.</param>
        /// <param name="stringLiterals">Collection of string literals that were stripped from the query text.</param>
        /// <param name="numericLiterals">Collection of numeric literals that were stripped from the query text.</param>
        /// <param name="leftHandAliasOfJoin">When parsing a JOIN, this is the schema that we are joining to.</param>
        private void Parse(string givenConditionText, Tokenizer tokenizer, string leftHandAliasOfJoin)
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

                            givenConditionText = Text.ReplaceRange(givenConditionText, startPos, endPos - startPos + 1, key);
                        }
                        else
                        {
                            //We have to store keys to point to the sub-expressions so we can know when we are done parsing.
                            var key = NextTempKey();
                            orLiterals.Add(key, subConditionText);

                            givenConditionText = Text.ReplaceRange(givenConditionText, startPos, endPos - startPos + 1, key);
                        }
                    }
                }
                else
                {
                    break;
                }
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

                        AddSubCondition(tokenizer, subCondition, leftHandAliasOfJoin);

                        givenConditionText = Text.ReplaceRange(givenConditionText, startPos, endPos - startPos + 1, VariableToKey(subExpressionKey));
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

        private static string ReplaceFirst(string text, string search, string replace)
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
                        var key = str.Substring(pos + 2, epos - pos - 2);
                        str = str.Replace(tok, key);
                    }
                    else break;
                }
                else break;
            }

            return str;
        }

        private void AddSubCondition(Tokenizer Tokenizer, SubCondition subCondition, string leftHandAliasOfJoin)
        {
            var logicalConnector = LogicalConnector.None;

            var tokenizer = new Tokenizer(subCondition.Expression);

            while (!tokenizer.IsExhausted())
            {
                int startPosition = tokenizer.Caret;

                string token = tokenizer.EatGetNext().ToLowerInvariant();

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

                    string left = QueryBatch.GetLiteralValue(token, out var leftDataType);

                    //Logical Qualifier
                    token = tokenizer.EatGetNext().ToLowerInvariant();
                    if (token.Is("not"))
                    {
                        token += " " + tokenizer.EatGetNext().ToLowerInvariant();
                    }

                    var logicalQualifier = StaticConditionHelpers.ParseLogicalQualifier(token);

                    //Righthand value:
                    string right = QueryBatch.GetLiteralValue(tokenizer.EatGetNext(), out var rightDataType);

                    int endPosition = tokenizer.Caret;

                    if (logicalQualifier == LogicalQualifier.Between || logicalQualifier == LogicalQualifier.NotBetween)
                    {
                        if (rightDataType != BasicDataType.Numeric)
                        {
                            throw new KbParserException($"Invalid token, Found [{right}] expected numeric value.");
                        }

                        string and = tokenizer.EatGetNext().ToLowerInvariant();
                        if (and.Is("and") == false)
                        {
                            throw new KbParserException($"Invalid token, Found [{and}] expected [and].");
                        }

                        var rightRange = tokenizer.EatGetNext().ToLowerInvariant();

                        if (Tokenizer.Literals.TryGetValue(rightRange, out var rightOfRange))
                        {
                            if (rightOfRange.DataType != BasicDataType.Numeric)
                            {
                                throw new KbParserException($"Invalid token, Found [{rightOfRange.Value}] expected numeric value.");
                            }

                            rightRange = rightOfRange.Value;
                        }

                        right = $"{right}:{rightRange}";

                        endPosition = tokenizer.Caret;
                    }

                    var condition = new Condition(conditionPlaceholder, logicalConnector, new SmartValue(left, leftDataType), logicalQualifier, new SmartValue(right, rightDataType));

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
            var clone = new Conditions(QueryBatch)
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

        #region Explain (Order of Operations)

        public static string FriendlyPlaceholder(string val) => val.ToUpper()
            .Replace($"{ConditionKeyMarker}_".ToUpper(), "Cond")
            .Replace($"{ExpressionKeyMarker}_".ToUpper(), "Expr");

        private static string Pad(int indentation)
            => "".PadLeft(indentation * 2, ' ');

        /// <summary>
        /// This function makes a (somewhat) user readable expression tree, used for debugging and explanations.
        /// It also demonstrates how we process the recursive condition logic.
        /// </summary>
        public string ExplainOperations(int indentation = 0)
        {
            var result = new StringBuilder();

            if (Root.ExpressionKeys.Count > 0)
            {
                //The root condition is just a pointer to a child condition, so get the "root" child condition.
                var rootCondition = SubConditionFromExpressionKey(Root.Key);
                ExplainSubOperations(ref result, rootCondition, indentation);
            }

            return result.ToString();
        }

        /// <summary>
        /// This function makes a (somewhat) user readable expression tree, used for debugging and explanations.
        /// It also demonstrates how we process the recursive condition logic.
        /// Called by parent ExplainConditionTree()
        /// </summary>
        private void ExplainSubOperations(ref StringBuilder result, SubCondition givenSubCondition, int indentation)
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
                    ExplainSubOperations(ref result, subCondition, indentation + 2);
                    result.AppendLine(Pad(indentation + 2) + ")");
                }

                result.AppendLine(Pad(indentation + 1) + ")");
            }
        }

        #endregion

        #region Explain (Flat)

        /// <summary>
        /// This function makes returns a string that represents how and where conditions are used to satisfy a query.
        /// </summary>
        public string ExplainFlat()
        {
            var result = new StringBuilder();

            if (Root.ExpressionKeys.Count > 0)
            {
                //The root condition is just a pointer to a child condition, so get the "root" child condition.
                var rootCondition = SubConditionFromExpressionKey(Root.Key);
                ExplainFlatSubCondition(ref result, rootCondition, 0);
            }

            return result.ToString();
        }

        private void ExplainFlatSubCondition(ref StringBuilder result, SubCondition givenSubCondition, int indentation)
        {
            foreach (var expressionKey in givenSubCondition.ExpressionKeys)
            {
                var subCondition = SubConditionFromExpressionKey(expressionKey);

                if (subCondition.Conditions.Count > 0)
                {
                    foreach (var condition in subCondition.Conditions)
                    {
                        result.AppendLine("• " + Pad(1 + indentation) + $"'{condition.Left}' {condition.LogicalQualifier} '{condition.Right}'.");
                    }
                }

                if (subCondition.ExpressionKeys.Count > 0)
                {
                    ExplainFlatSubCondition(ref result, subCondition, indentation + 1);
                }
            }
        }

        #endregion
    }
}
