using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Query.Tokenizers;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Query.Constraints
{
    internal class Conditions
    {
        private int _lastNumber = 0;

        public int Number { get => _lastNumber; }

        public List<SubCondition> SubConditions { get; private set; } = new();

        /// Every condition instance starts with a single root node that all others point back to given some lineage.
        /// This is the name of the root node.
        public string RootSubConditionKey { get; private set; } = string.Empty;

        public string HighLevelConditionTree { get; private set; } = string.Empty;

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

        public IEnumerable<SubCondition> NonRootSubConditions => SubConditions.Where(o => !o.IsRoot);

        private static string VariableToKey(string str)
        {
            return $"$:{str}$";
        }

        private string GetNextVariableLetter()
        {
            return (_lastNumber++).ToString();
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
                        string subConditionVariable = $"s_{GetNextVariableLetter()}"; //S=SubCondition-Key
                        string subConditionText = conditionsText.Substring(startPos, endPos - startPos + 1).Trim();
                        string parenTrimmedSubConditionText = subConditionText.Substring(1, subConditionText.Length - 2).Trim();
                        var subCondition = new SubCondition(subConditionVariable, parenTrimmedSubConditionText);
                        AddSubCondition(literalStrings, subCondition, leftHandAlias);
                        conditionsText = conditionsText.Replace(subConditionText, VariableToKey(subConditionVariable));
                    }
                }
                else
                {
                    break;
                }
            }

            RootSubConditionKey = conditionsText.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            RemoveVariableMarkers();

            //Mark the root SubCondition as such.
            SubConditions.Single(o => o.SubConditionKey == RootSubConditionKey).IsRoot = true;

            HighLevelConditionTree = BuildHighLevelConditionTree();

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
            RootSubConditionKey = RemoveVariableMarker(RootSubConditionKey);

            foreach (var subCondition in SubConditions)
            {
                subCondition.Condition = RemoveVariableMarker(subCondition.Condition);
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

        public SubCondition SubConditionByKey(string key)
        {
            return SubConditions.First(o => o.SubConditionKey == key);
        }

        public Conditions Clone()
        {
            var clone = new Conditions()
            {
                RootSubConditionKey = RootSubConditionKey,
                HighLevelConditionTree = HighLevelConditionTree
            };

            clone.AllFields.AddRange(AllFields);

            foreach (var subCondition in SubConditions)
            {
                var subConditionClone = new SubCondition(subCondition.SubConditionKey, subCondition.Condition)
                {
                    IsRoot = subCondition.IsRoot,
                };

                foreach (var condition in subCondition.Conditions)
                {
                    subConditionClone.Conditions.Add(condition.Clone());
                }
                subConditionClone.ConditionKeys.UnionWith(subCondition.ConditionKeys);
                subConditionClone.SubConditionKeys.UnionWith(subCondition.SubConditionKeys);

                clone.SubConditions.Add(subConditionClone);
            }

            clone.Hash = clone.BuildConditionHash();

            return clone;
        }

        public void AddSubCondition(KbInsensitiveDictionary<string> literalStrings, SubCondition subCondition, string leftHandAlias)
        {
            var logicalConnector = LogicalConnector.None;

            var conditionTokenizer = new ConditionTokenizer(subCondition.Condition);

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
                        subCondition.ConditionKeys.Add(token.Substring(2, token.Length - 3));
                    }
                    else if (token.StartsWith("$:s"))
                    {
                        subCondition.SubConditionKeys.Add(token.Substring(2, token.Length - 3));
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

                    var condition = new Condition(subCondition.SubConditionKey, conditionKey, logicalConnector, left, logicalQualifier, right);

                    subCondition.Condition = subCondition.Condition.Remove(startPosition, endPosition - startPosition);
                    subCondition.Condition = subCondition.Condition.Insert(startPosition, VariableToKey(conditionKey) + " ");

                    conditionTokenizer.SetText(subCondition.Condition, 0);

                    subCondition.Conditions.Add(condition);
                    logicalConnector = LogicalConnector.None;
                }
            }

            subCondition.Condition = subCondition.Condition.Replace(" or ", " || ").Replace(" and ", " && ").Trim();

            SubConditions.Add(subCondition);
        }

        #region Debug.

        public string BuildFullVirtualCondition()
        {
            var result = new StringBuilder();
            result.AppendLine($"[{RootSubConditionKey}]");

            if (Root.SubConditionKeys.Count > 0)
            {
                result.AppendLine("(");

                foreach (var subConditionKey in Root.SubConditionKeys)
                {
                    var subCondition = SubConditionByKey(subConditionKey);
                    result.AppendLine($"  [{subCondition.Condition}]");
                    BuildFullVirtualCondition(ref result, subCondition, 1);
                }

                result.AppendLine(")");
            }

            return result.ToString();
        }

        private void BuildFullVirtualCondition(ref StringBuilder result, SubCondition conditionSubCondition, int depth)
        {
            //If we have SubConditions, then we need to satisfy those in order to complete the equation.
            foreach (var subConditionKey in conditionSubCondition.SubConditionKeys)
            {
                var subCondition = SubConditionByKey(subConditionKey);
                result.AppendLine("".PadLeft((depth) * 2, ' ') + $"[{subCondition.Condition}]");

                if (subCondition.SubConditionKeys.Count > 0)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + "(");
                    BuildFullVirtualCondition(ref result, subCondition, depth + 1);
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + ")");
                }
            }

            if (conditionSubCondition.Conditions.Count > 0)
            {
                result.AppendLine("".PadLeft((depth + 1) * 1, ' ') + "(");
                foreach (var condition in conditionSubCondition.Conditions)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + $"{condition.ConditionKey}: {condition.Left} {condition.LogicalQualifier}");
                }
                result.AppendLine("".PadLeft((depth + 1) * 1, ' ') + ")");
            }
        }

        #endregion

        public List<PrefixedField> AllFields { get; private set; } = new();

        /// <summary>
        /// This function is used to build a logical condition at the SubCondition
        ///     level, it also demonstrates how we process the recursive logic.
        /// </summary>
        /// <returns></returns>
        public string BuildHighLevelConditionTree()
        {
            var condition = new StringBuilder($"({Root.Condition})");

            foreach (var subConditionKey in Root.SubConditionKeys)
            {
                var subCondition = SubConditionByKey(subConditionKey);
                condition.Replace(subConditionKey, $"({subCondition.Condition})");
                BuildHighLevelConditionTree(ref condition, subCondition);
            }

            return condition.ToString();
        }

        public void BuildHighLevelConditionTree(ref StringBuilder condition, SubCondition conditionSubCondition)
        {
            foreach (var subConditionKey in conditionSubCondition.SubConditionKeys)
            {
                var subCondition = SubConditionByKey(subConditionKey);
                condition.Replace(subConditionKey, $"({subCondition.Condition})");
                BuildHighLevelConditionTree(ref condition, subCondition);
            }
        }

        public string BuildConditionHash()
        {
            var result = new StringBuilder();
            result.Append($"[{RootSubConditionKey}]");

            if (Root?.SubConditionKeys?.Count > 0)
            {
                result.Append('(');

                foreach (var subConditionKey in Root.SubConditionKeys)
                {
                    var subCondition = SubConditionByKey(subConditionKey);
                    result.Append($"[{subCondition.Condition}]");
                    BuildConditionHash(ref result, subCondition, 1);
                }

                result.Append(')');
            }

            return Library.Helpers.ComputeSHA256(result.ToString());
        }

        private void BuildConditionHash(ref StringBuilder result, SubCondition conditionSubCondition, int depth)
        {
            //If we have SubConditions, then we need to satisfy those in order to complete the equation.
            foreach (var subConditionKey in conditionSubCondition.SubConditionKeys)
            {
                var subCondition = SubConditionByKey(subConditionKey);
                result.Append($"[{subCondition.Condition}]");

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

                if (subCondition.SubConditionKeys.Count > 0)
                {
                    result.Append('(');
                    BuildConditionHash(ref result, subCondition, depth + 1);
                    result.Append(')');
                }
            }

            if (conditionSubCondition.Conditions.Count > 0)
            {
                result.Append('(');
                foreach (var condition in conditionSubCondition.Conditions)
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
