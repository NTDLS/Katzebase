using Katzebase.Engine.Documents;
using Katzebase.Library;
using Katzebase.Library.Client.Management;
using NCalc;
using Newtonsoft.Json.Linq;
using System;
using System.Linq.Expressions;
using System.Security.Cryptography;
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


        internal ConditionSubset? SubsetByUID(Guid uid)
        {
            foreach (var subset in Subsets)
            {
                if (subset.UID == uid)
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
                if (subset.UID == uid)
                {
                    return subset;
                }

                var result = SubsetByUID(subset, subset.UID);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        internal ConditionSubset? GetNext(ref ConditionConsumptionTracker tracker)
        {
            foreach (var subset in Subsets)
            {
                if (tracker.ConsumedSubsets.Contains(subset.UID) == false)
                {
                    tracker.ConsumedSubsets.Add(subset.UID);
                    return subset;
                }

                var result = GetNext(subset, ref tracker);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private ConditionSubset? GetNext(ConditionSubset rootSubset, ref ConditionConsumptionTracker tracker)
        {
            foreach (var subset in rootSubset.Conditions.OfType<ConditionSubset>())
            {
                if (tracker.ConsumedSubsets.Contains(subset.UID) == false)
                {
                    tracker.ConsumedSubsets.Add(subset.UID);
                    return subset;
                }

                var result = GetNext(subset, ref tracker);
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

        #region Debug.

        public void DebugPrint()
        {
            foreach (var subset in Subsets)
            {
                Console.WriteLine(LogicalConnectorToString(subset.LogicalConnector) + " (");
                foreach (var condition in subset.Conditions)
                {
                    DebugPrintCondition(condition, 1);
                }
                Console.WriteLine(")");
            }
        }

        private void DebugPrintCondition(ICondition condition, int depth)
        {
            if (condition is ConditionSubset)
            {
                Console.WriteLine("".PadLeft(depth, '\t') + "(");
                var subset = (ConditionSubset)condition;
                foreach (var subsetCondition in subset.Conditions)
                {
                    DebugPrintCondition(subsetCondition, depth + 1);
                }

                Console.WriteLine("".PadLeft(depth, '\t') + ")");
            }
            else if (condition is ConditionSingle)
            {
                var single = (ConditionSingle)condition;
                Console.Write("".PadLeft(depth, '\t') + LogicalConnectorToString(single.LogicalConnector));
                Console.WriteLine($"[{single.Left}] {LogicalQualifierToString(single.LogicalQualifier)} [{single.Right}]");
            }
        }

        #endregion

        public string BuildSubsetExpressionTree()
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

            return expression.ToString();
        }

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

        public string BuildFullExpressionTree()
        {
            var expression = new StringBuilder();

            foreach (var subset in Subsets)
            {
                var connectorString = LogicalConnectorToString(subset.LogicalConnector);
                expression.Append(string.IsNullOrWhiteSpace(connectorString) ? "" : $" {connectorString} ");

                expression.Append('(');
                foreach (var condition in subset.Conditions)
                {
                    BuildFullExpressionTree(condition, ref expression);
                }
                expression.Append(')');
            }

            return expression.ToString();
        }

        private void BuildFullExpressionTree(ICondition condition, ref StringBuilder expression)
        {
            if (condition is ConditionSubset)
            {
                expression.Append('(');
                var subset = (ConditionSubset)condition;
                foreach (var subsetCondition in subset.Conditions)
                {
                    BuildFullExpressionTree(subsetCondition, ref expression);
                }

                expression.Append(')');
            }
            else if (condition is ConditionSingle)
            {
                var single = (ConditionSingle)condition;
                var connectorString = LogicalConnectorToString(single.LogicalConnector);
                expression.Append(string.IsNullOrWhiteSpace(connectorString) ? "" : $" {connectorString} ");
                expression.Append($"[{single.Left}] {LogicalQualifierToString(single.LogicalQualifier)} [{single.Right}]");
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

        /*
        public void AddRange(Conditions conditions)
        {
            Groups = conditions.Groups;
            foreach (Condition condition in conditions.Collection)
            {
                Add(condition);
            }
        }

        public Condition Add(Condition condition)
        {
            var result = new Condition(condition.LogicalConnector, condition.Field, condition.ConditionQualifier, condition.Value);
            result.Children.AddRange(condition.Children);
            Collection.Add(result);
            return result;
        }
        */
    }
}
