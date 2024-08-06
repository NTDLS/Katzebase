using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Schemas;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Query.Constraints
{
    internal class ExpressionOptimization
    {
        /// <summary>
        /// A list of the indexes that have been selected by the optimizer for the specified conditions.
        /// </summary>
        public List<IndexSelection> IndexSelection { get; private set; } = new();

        /// <summary>
        /// A clone of the conditions that this set of index selections was built for.
        /// Also contains the indexes associated with each SubExpression of conditions.
        /// </summary>
        public Conditions Conditions { get; private set; }

        private readonly Transaction _transaction;

        public ExpressionOptimization(Transaction transaction, Conditions conditions)
        {
            _transaction = transaction;
            Conditions = conditions.Clone();
        }

        #region Builder.

        /// <summary>
        /// Takes a nested set of conditions and returns a selection of indexes as well as a clone of the conditions with associated indexes.
        /// </summary>
        /// <returns>A selection of indexes as well as a clone of the conditions with associated indexes</returns>
        public static ExpressionOptimization Build(EngineCore core,
            Transaction transaction, PhysicalSchema physicalSchema, Conditions conditions, string workingSchemaPrefix)
        {
            try
            {
                /* This still has condition values in it, that wont work. *Face palm*
                var cacheItem = core.LookupOptimizationCache.Get(conditions.Hash) as MSQConditionLookupOptimization;
                if (cacheItem != null)
                {
                    return cacheItem;
                }
                */

                var indexCatalog = core.Indexes.AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                var lookupOptimization = new ExpressionOptimization(transaction, conditions);

                foreach (var subExpression in conditions.SubExpressions)
                {
                    if (subExpression.Expressions.Any(o => o.Left.Prefix != workingSchemaPrefix))
                    {
                        if (subExpression.Expressions.Any(o => o.LogicalConnector != LogicalConnector.And) == false)
                        {
                            //We can't yet figure out how to eliminate documents if the conditions are for more
                            //..    than one schema and all of the logical connectors are not AND. This can be done however.
                            //  We just generally have a lot of optimization trouble with ORs.
                            continue;
                        }
                    }

                    var potentialIndexes = new List<PotentialIndex>();

                    //Loop though each index in the schema.
                    foreach (var physicalIndex in indexCatalog.Collection)
                    {
                        var handledKeyNames = new List<PrefixedField>();

                        for (int i = 0; i < physicalIndex.Attributes.Count; i++)
                        {
                            if (physicalIndex.Attributes == null || physicalIndex.Attributes[i] == null)
                            {
                                throw new KbNullException($"Value should not be null {nameof(physicalIndex.Attributes)}.");
                            }

                            var keyName = physicalIndex.Attributes[i].Field?.ToLowerInvariant();
                            if (keyName == null)
                            {
                                throw new KbNullException($"Value should not be null {nameof(keyName)}.");
                            }

                            var matchedNonConvertedConditions =
                                subExpression.Expressions.Where(o => o.CoveredByIndex == false
                                    && o.Left.Value == keyName && o.Left.Prefix == workingSchemaPrefix);

                            foreach (var matchedCondition in matchedNonConvertedConditions)
                            {
                                handledKeyNames.Add(PrefixedField.Parse(matchedCondition.Left.Key));
                            }

                            if (matchedNonConvertedConditions.Any() == false)
                            {
                                break;
                            }
                        }

                        if (handledKeyNames.Count > 0)
                        {
                            var potentialIndex = new PotentialIndex(physicalIndex, handledKeyNames);
                            potentialIndexes.Add(potentialIndex);
                        }
                    }

                    //Grab the index that matches the most of our supplied keys but also has the least attributes.
                    var firstIndex = (from o in potentialIndexes where o.Tried == false select o)
                        .OrderByDescending(s => s.CoveredFields.Count)
                        .ThenBy(t => t.Index.Attributes.Count).FirstOrDefault();

                    if (firstIndex != null)
                    {
                        var handledKeys = GetConvertedConditions(subExpression.Expressions, firstIndex.CoveredFields);

                        //Where the left value is in the covered fields:

                        //var handledKeys = (from o in SubExpression.Expressions where firstIndex.CoveredFields.Contains(o.Left.Value ?? string.Empty) select o).ToList();
                        foreach (var handledKey in handledKeys)
                        {
                            handledKey.CoveredByIndex = true;
                        }

                        firstIndex.SetTried();

                        var indexSelection = new IndexSelection(firstIndex.Index, firstIndex.CoveredFields);

                        lookupOptimization.IndexSelection.Add(indexSelection);

                        //Mark which condition this index selection satisfies.
                        var sourceSubExpression = lookupOptimization.Conditions.SubExpressionByKey(subExpression.SubExpressionKey);
                        sourceSubExpression.IndexSelection = indexSelection;

                        foreach (var condition in sourceSubExpression.Expressions)
                        {
                            if (indexSelection.CoveredFields.Any(o => o.Key == condition.Left.Key))
                            {
                                condition.CoveredByIndex = true;
                            }
                        }
                    }
                }

                //core.LookupOptimizationCache.Add(conditions.Hash, lookupOptimization, DateTime.Now.AddMinutes(10));

                //When we get here, we have one index that seems to want to cover multiple tables - no cool man. Not cool.

                return lookupOptimization;
            }
            catch (Exception ex)
            {
                Interactions.Management.LogManager.Error($"Failed to select indexes for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        public static List<ConditionExpression> GetConvertedConditions(
            List<ConditionExpression> expressions, List<PrefixedField> coveredFields)
        {
            var result = new List<ConditionExpression>();

            foreach (var coveredField in coveredFields)
            {
                foreach (var condition in expressions)
                {
                    if (condition.Left.Key == coveredField.Key)
                    {
                        result.Add(condition);
                    }
                }
            }

            return result;
        }

        public static bool CanApplyIndexing(ConditionSubExpression subExpression)
        {
            //Currently we can only use a partial index match if all of the conditions in a group are "AND"s,
            //  so if we have an "OR" and any of the conditions are not covered then skip the indexing.
            if (subExpression.Expressions.Any(o => o.LogicalConnector == LogicalConnector.Or)
                || subExpression.Expressions.Any(o => o.CoveredByIndex == true) == false)
            {
                return false;
            }
            return true;
        }

        private bool? _canApplyIndexingResultCached = null;

        public bool CanApplyIndexing()
        {
            if (_canApplyIndexingResultCached != null)
            {
                return (bool)_canApplyIndexingResultCached;
            }

            if (Conditions.NonRootSubExpressions.Any(o => o.IndexSelection == null) == false)
            {
                //All condition SubExpressions have a selected index.
                foreach (var subExpression in Conditions.NonRootSubExpressions)
                {
                    if (CanApplyIndexing(subExpression) == false)
                    {
                        _canApplyIndexingResultCached = false;
                        return false;
                    }
                }

                #region Index usage reporting.

                /*
                if (explain)
                {
                    //var message = new StringBuilder();
                    //var friendlyExpression = new StringBuilder();
                    //BuildFullVirtualExpression(ref friendlyExpression, Expressions.Root, 0);
                    //message.AppendLine(friendlyExpression.ToString());

                    message.AppendLine($"Expression: ({friendlyExpression}) {{");

                    message.AppendLine($"Applying {IndexSelection.Count} index(s).");

                    foreach (var index in IndexSelection)
                    {
                        var coveredFields = string.Join("', '", index.CoveredFields.Select(o => o.Key)).Trim();
                        message.AppendLine($"Index '{index.PhysicalIndex.Name}' covers {coveredFields}");
                    }

                    //All condition SubExpressions have a selected index. Start building a list of possible document IDs.
                    foreach (var subExpression in Conditions.NonRootSubExpressions)
                    {
                        message.AppendLine($"Expression: ({FriendlyExpression(subExpression.Expression)}) {{");

                        foreach (var condition in subExpression.Expressions)
                        {
                            string leftIndex = string.Empty;
                            string rightIndex = string.Empty;

                            if (condition.CoveredByIndex)
                            {
                                foreach (var index in IndexSelection)
                                {
                                    if (index.CoveredFields.Any(o => o.Key == condition.Left.Key))
                                    {
                                        leftIndex = index.PhysicalIndex.Name;
                                    }
                                    if (index.CoveredFields.Any(o => o.Key == condition.Right.Key))
                                    {
                                        rightIndex = index.PhysicalIndex.Name;
                                    }
                                }
                            }

                            string leftValue = condition.Left.IsConstant ? $"'{condition.Left.Key}'" : condition.Left.Key;
                            string rightValue = condition.Right.IsConstant ? $"'{condition.Right.Key}'" : condition.Right.Key;

                            string indexInfo = string.Empty;

                            if (string.IsNullOrEmpty(leftIndex) == false || string.IsNullOrEmpty(rightIndex) == false)
                            {
                                indexInfo += ", Indexes (";

                                if (string.IsNullOrEmpty(leftIndex) == false)
                                {
                                    indexInfo += $"Left: [{leftIndex}] ";
                                }

                                if (string.IsNullOrEmpty(rightIndex) == false)
                                {
                                    indexInfo += $"Right: [{rightIndex}] ";
                                }

                                indexInfo = indexInfo.Trim();

                                indexInfo += ")";
                            }

                            message.AppendLine($"\t'{FriendlyExpression(condition.ConditionKey)}: ({leftValue} {condition.LogicalQualifier} {rightValue}){indexInfo}");
                        }
                        message.AppendLine("}");
                    }

                    //_transaction.AddMessage(message.ToString(), KbMessageType.Explain);
                }
                */

                #endregion

                _canApplyIndexingResultCached = true;
                return true;
            }
            _canApplyIndexingResultCached = false;
            return false;
        }

        #region Optimization explanation.

        static string FriendlyExpression(string val) => val.ToUpper()
            .Replace("C_", "Expr")
            .Replace("S_", "SubExpr");

        public string BuildFullVirtualExpression()
        {
            if (Conditions.SubExpressions.Count == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder();
            result.AppendLine($"[{FriendlyExpression(Conditions.RootSubExpressionKey)}]"
                + (CanApplyIndexing() ? " {Indexable}" : " {non-Indexable}"));

            if (Conditions.Root.SubExpressionKeys.Count > 0)
            {
                result.AppendLine("(");

                foreach (var subExpressionKey in Conditions.Root.SubExpressionKeys)
                {
                    var subExpression = Conditions.SubExpressionByKey(subExpressionKey);
                    result.AppendLine($"  [{FriendlyExpression(subExpression.Expression)}]"
                        + (CanApplyIndexing(subExpression) ? " {Indexable (" + subExpression.IndexSelection?.PhysicalIndex.Name + ")}" : " {non-Indexable}"));

                    result.AppendLine("  (");
                    BuildFullVirtualExpression(ref result, subExpression, 1);
                    result.AppendLine("  )");
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
                var subExpression = Conditions.SubExpressionByKey(subExpressionKey);
                result.AppendLine("".PadLeft(depth * 4, ' ')
                    + $"[{FriendlyExpression(subExpression.Expression)}]" + (CanApplyIndexing(subExpression) ? " {Indexable (" + subExpression.IndexSelection?.PhysicalIndex.Name + ")}" : " {non-Indexable}"));

                if (subExpression.Expressions.Count > 0)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + FriendlyExpression(subExpressionKey) + "->" + "(");
                    foreach (var condition in subExpression.Expressions)
                    {
                        result.AppendLine("".PadLeft((depth + 1) * 4, ' ')
                            + $"{FriendlyExpression(condition.ConditionKey)}: ({condition.Left} {condition.LogicalQualifier} {condition.Right})");
                    }
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + ")");
                }

                if (subExpression.SubExpressionKeys.Count > 0)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + "(");
                    BuildFullVirtualExpression(ref result, subExpression, depth + 1);
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + ")");
                }
            }

            if (conditionSubExpression.Expressions.Count > 0)
            {
                result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + "(");
                foreach (var condition in conditionSubExpression.Expressions)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 4, ' ') + $"{FriendlyExpression(condition.ConditionKey)}: ({condition.Left} {condition.LogicalQualifier} {condition.Right})");
                }
                result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + ")");
            }
        }

        #endregion
    }
}
