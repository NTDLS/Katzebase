using Katzebase.Engine.Indexes;
using System.Text;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class ConditionLookupOptimization
    {
        /// <summary>
        /// A list of the indexes that have been selected by the optimizer for the specified conditions.
        /// </summary>
        public List<IndexSelection> IndexSelection { get; set; } = new();

        /// <summary>
        /// A clone of the conditions that this set of index selections was built for. Also contains the indexes associated with each subset of conditions.
        /// </summary>
        public Conditions Conditions { get; private set; }

        /// <summary>
        /// A flattened list of conditions, used to build the index selections (not used outside of index selection algorithms).
        /// </summary>
        //public List<FlatConditionGroup> FlatConditionGroups { get; private set; } = new();

        public ConditionLookupOptimization(Conditions conditions)
        {
            Conditions = conditions.Clone();
        }

        public bool CanApplyIndexing(ConditionSubset subset)
        {
            //Currently we can only use a partial index match if all of the conditions in a group are "AND"s,
            //  so if we have an "OR" and any of the conditions are not covered then skip the indexing.
            if (subset.Conditions.Any(o => o.LogicalConnector == LogicalConnector.Or) && subset.Conditions.Any(o => o.CoveredByIndex == false))
            {
                return false;
            }
            return true;
        }

        public bool CanApplyIndexing()
        {
            if (Conditions.NonRootSubsets.Any(o => o.IndexSelection == null) == false)
            {
                //All condition subsets have a selected index. Start building a list of possible document IDs.
                foreach (var subset in Conditions.NonRootSubsets)
                {
                    if (CanApplyIndexing(subset) == false)
                    {
                        return false;
                    }

                }
                return true;
            }

            return false;
        }

        #region Debug

        public string BuildFullVirtualExpression()
        {
            var result = new StringBuilder();
            result.AppendLine($"[{Conditions.RootSubsetKey}]" + (CanApplyIndexing() ? " {Indexable}" : " {non-Indexable}"));

            if (Conditions.Root.SubsetKeys.Count > 0)
            {
                result.AppendLine("(");

                foreach (var subsetKey in Conditions.Root.SubsetKeys)
                {
                    var subset = Conditions.SubsetByKey(subsetKey);
                    result.AppendLine($"  [{subset.Expression}]" + (CanApplyIndexing(subset) ? " {Indexable (" + subset.IndexSelection?.Index.Name + ")}" : " {non-Indexable}"));
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
                var subset = Conditions.SubsetByKey(subsetKey);
                result.AppendLine("".PadLeft((depth) * 2, ' ') + $"[{subset.Expression}]" + (CanApplyIndexing(subset) ? " {Indexable (" + subset.IndexSelection?.Index.Name + ")}" : " {non-Indexable}"));

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
    }
}
