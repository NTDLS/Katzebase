using System.Text;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class Conditions
    {
        public List<ConditionSubset> Subsets { get; set; } = new();

        private string _lastVariableLetter = "";

        public string GetNextVariableLetter()
        {
            if (_lastVariableLetter == string.Empty)
            {
                _lastVariableLetter = "A";
                return _lastVariableLetter;
            }
            char[] chars = _lastVariableLetter.ToCharArray();
            char lastChar = chars[chars.Length - 1];
            char nextChar = (char)(lastChar + 1);

            if (nextChar > 'Z')
            {
                _lastVariableLetter = _lastVariableLetter + "A";
            }
            else
            {
                chars[chars.Length - 1] = nextChar;
                _lastVariableLetter = new string(chars);
            }
            return _lastVariableLetter;
        }

        public void FillInSubsetVariableNames()
        {
            foreach (var subset in Subsets)
            {
                subset.SubsetVariableName = GetNextVariableLetter();
                FillInSubsetVariableNames(subset);
            }
        }

        private void FillInSubsetVariableNames(ConditionSubset rootSubset)
        {
            foreach (var subset in rootSubset.Conditions.OfType<ConditionSubset>())
            {
                subset.SubsetVariableName = GetNextVariableLetter();
                FillInSubsetVariableNames(subset);
            }
        }

        public ConditionSubset? SubsetByUID(Guid uid)
        {
            foreach (var subset in Subsets)
            {
                if (subset.SubsetUID == uid)
                {
                    return subset;
                }

                var result = SubsetByUID(subset, uid);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private ConditionSubset? SubsetByUID(ConditionSubset rootSubset, Guid uid)
        {
            foreach (var subset in rootSubset.Conditions.OfType<ConditionSubset>())
            {
                if (subset.SubsetUID == uid)
                {
                    return subset;
                }

                var result = SubsetByUID(subset, subset.SubsetUID);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a flattened list of conditions groups from nested conditions.
        /// </summary>
        /// <returns></returns>
        public List<FlatConditionGroup> Flatten()
        {
            var flattenedGroups = new List<FlatConditionGroup>();

            foreach (var subset in Subsets)
            {
                var flatGroup = new FlatConditionGroup(subset);
                flattenedGroups.Add(flatGroup);

                foreach (var condition in subset.Conditions)
                {
                    Flatten(condition, ref flatGroup, ref flattenedGroups);
                }
            }

            return flattenedGroups;
        }

        /// <summary>
        /// Recursive counterpart of Flatten.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="flatGroup"></param>
        /// <param name="flattenedGroups"></param>
        private void Flatten(ICondition condition, ref FlatConditionGroup flatGroup, ref List<FlatConditionGroup> flattenedGroups)
        {
            if (condition is ConditionSubset)
            {
                var subset = (ConditionSubset)condition;

                var subsetFlatGroup = new FlatConditionGroup(subset);
                flattenedGroups.Add(subsetFlatGroup);

                foreach (var subsetCondition in subset.Conditions)
                {
                    Flatten(subsetCondition, ref subsetFlatGroup, ref flattenedGroups);
                }
            }
            else if (condition is ConditionSingle)
            {
                flatGroup.Conditions.Add((ConditionSingle)condition);
            }
        }

        /// <summary>
        /// Builds a mathematical expression tree that can be used to evaluate if all subset conditions have been met.
        /// </summary>
        /// <returns></returns>
        public string BuildSubsetExpressionTree()
        {
            while (true)
            {
                var expression = new StringBuilder();

                foreach (var subset in Subsets)
                {
                    expression.Append(LogicalConnectorToLogicString(subset.LogicalConnector));
                    expression.Append('(');
                    expression.Append(subset.SubsetVariableName);
                    BuildSubsetExpressionTree(subset, ref expression);
                    expression.Append(')');
                }

            }

            //return expression.ToString();
            return "";
        }

        /// <summary>
        /// Recursive counterpart to BuildSubsetExpressionTree()
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="expression"></param>

        private void BuildSubsetExpressionTree(ConditionSubset condition, ref StringBuilder expression)
        {
            foreach (var subset in condition.Conditions.OfType<ConditionSubset>())
            {
                expression.Append(LogicalConnectorToLogicString(condition.LogicalConnector));
                expression.Append('(');
                expression.Append(subset.SubsetVariableName);
                BuildSubsetExpressionTree(subset, ref expression);
                expression.Append(')');
            }
        }

        /// <summary>
        /// Builds a full expression tree, just used for debugging.
        /// </summary>
        /// <returns></returns>
        public string BuildFullExpressionTree()
        {
            var expression = new StringBuilder();

            foreach (var subset in Subsets)
            {
                var connectorString = LogicalConnectorToString(subset.LogicalConnector);
                expression.AppendLine(string.IsNullOrWhiteSpace(connectorString) ? "" : $" {connectorString} ");

                expression.AppendLine("(");
                expression.AppendLine($"/*{subset.SubsetVariableName}*/");
                foreach (var condition in subset.Conditions)
                {
                    BuildFullExpressionTree(condition, ref expression);
                }
                expression.AppendLine(")");
            }

            return expression.ToString();
        }

        /// <summary>
        /// Recursive counterpart to BuildFullExpressionTree.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="expression"></param>
        private void BuildFullExpressionTree(ICondition condition, ref StringBuilder expression)
        {
            if (condition is ConditionSubset)
            {
                var subset = (ConditionSubset)condition;
                expression.AppendLine("(");
                expression.AppendLine($"/*{subset.SubsetVariableName}*/");
                foreach (var subsetCondition in subset.Conditions)
                {
                    BuildFullExpressionTree(subsetCondition, ref expression);
                }

                expression.AppendLine(")");
            }
            else if (condition is ConditionSingle)
            {
                var single = (ConditionSingle)condition;
                var connectorString = LogicalConnectorToString(single.LogicalConnector);
                expression.Append(string.IsNullOrWhiteSpace(connectorString) ? "" : $" {connectorString} ");
                expression.AppendLine($"[{single.Left}] {LogicalQualifierToString(single.LogicalQualifier)} [{single.Right}]");
            }
        }

        public Conditions Clone()
        {
            var result = new Conditions();

            foreach (var subset in Subsets)
            {
                //Yes, this is recursive (though the interface).
                result.Subsets.Add((ConditionSubset)subset.Clone());
            }

            return result;
        }
    }
}
