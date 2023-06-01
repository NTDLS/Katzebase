using Katzebase.Engine.Documents;
using Katzebase.Library;
using Katzebase.Library.Client.Management;
using Newtonsoft.Json.Linq;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class Conditions
    {
        public List<ConditionSubset> Subsets { get; set; } = new();

        internal ConditionSubset? SubsetByUID(Guid uid)
        {
            foreach (var subset in this.Subsets)
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
            foreach (var subset in this.Subsets)
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

            foreach (var subset in this.Subsets)
            {
                var flatGroup = new FlatConditionGroup(subset.LogicalConnector, subset.UID);
                flattenedGroups.Add(flatGroup);

                Console.WriteLine(DebugLogicalConnectorToString(subset.LogicalConnector) + " (");
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

                var subsetFlatGroup = new FlatConditionGroup(subset.LogicalConnector, subset.UID);
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
            foreach (var subset in this.Subsets)
            {
                Console.WriteLine(DebugLogicalConnectorToString(subset.LogicalConnector) + " (");
                foreach (var condition in subset.Conditions)
                {
                    DebugPrintCondition(condition, 1);
                }
                Console.WriteLine(")");
            }
        }

        private string DebugLogicalConnectorToString(LogicalConnector logicalConnector)
        {
            return (logicalConnector == LogicalConnector.None ? "" : logicalConnector.ToString().ToUpper() + " ");
        }

        private string DebugLogicalQualifierToString(LogicalQualifier logicalQualifier)
        {
            switch (logicalQualifier)
            {
                case LogicalQualifier.Equals:
                    return "=";
                case LogicalQualifier.NotEquals:
                    return "!=";
                case LogicalQualifier.GreaterThanOrEqual:
                    return ">=";
                case LogicalQualifier.LessThanOrEqual:
                    return "<=";
                case LogicalQualifier.LessThan:
                    return "<";
                case LogicalQualifier.GreaterThan:
                    return ">";
            }

            return "";
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
                Console.Write("".PadLeft(depth, '\t') + DebugLogicalConnectorToString(single.LogicalConnector));
                Console.WriteLine($"[{single.Left}] {DebugLogicalQualifierToString(single.LogicalQualifier)} [{single.Right}]");
            }
        }

        #endregion

        public Conditions Clone()
        {
            var result = new Conditions();

            foreach (var subset in this.Subsets)
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

        public bool IsMatch(PersistDocument persistDocument)
        {
            Utility.EnsureNotNull(persistDocument);
            Utility.EnsureNotNull(persistDocument.Content);

            JObject jsonContent = JObject.Parse(persistDocument.Content);

            return IsMatch(jsonContent);
        }

        public bool IsMatch(JObject jsonContent)
        {
            bool fullAttributeMatch = true;

            //Loop though each condition in the prepared query:
            foreach (var condition in Collection)
            {
                //Get the value of the condition:
                if (jsonContent.TryGetValue(condition.Field, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                {
                    //If the condition does not match the value in the document then we break from checking the remainder of the conditions for this document and continue with the next document.
                    //Otherwise we continue to the next condition until all conditions are matched.
                    if (condition.IsMatch(jToken.ToString().ToLower()) == false)
                    {
                        fullAttributeMatch = false;
                        break;
                    }
                }
            }

            return fullAttributeMatch;
        }
        */
    }
}
