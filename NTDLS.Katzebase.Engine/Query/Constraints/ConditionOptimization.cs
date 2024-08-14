using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Shared;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Query.Constraints
{
    internal class ConditionOptimization
    {
        /// <summary>
        /// A list of the indexes that have been selected by the optimizer for the specified conditions.
        /// </summary>
        public List<IndexSelection> IndexSelection { get; private set; } = new();

        /// <summary>
        /// A clone of the conditions that this set of index selections was built for.
        /// Also contains the indexes associated with each SubCondition of conditions.
        /// </summary>
        public Conditions Conditions { get; private set; }

        private readonly Transaction _transaction;

        public ConditionOptimization(Transaction transaction, Conditions conditions)
        {
            _transaction = transaction;
            Conditions = conditions.Clone();
        }

        #region Builder.

        /// <summary>
        /// Takes a nested set of conditions and returns a clone of the conditions with associated selection of indexes.
        /// </summary>
        public static ConditionOptimization BuildTree(EngineCore core, Transaction transaction,
            PhysicalSchema physicalSchema, Conditions allConditions, string workingSchemaPrefix)
        {
            var optimization = new ConditionOptimization(transaction, allConditions);

            var indexCatalog = core.Indexes.AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

            if (optimization.Conditions.Root.ExpressionKeys.Count > 0)
            {
                //The root condition is just a pointer to a child condition, so get the "root" child condition.
                var rootCondition = optimization.Conditions.SubConditionFromKey(optimization.Conditions.Root.Key);
                if (!BuildTree(optimization, core, transaction, indexCatalog, physicalSchema, optimization.Conditions, workingSchemaPrefix, rootCondition))
                {
                    //Invalidate indexing optimization.
                    return new ConditionOptimization(transaction, allConditions);
                }
            }

            return optimization;
        }

        /// <summary>
        /// Takes a nested set of conditions and returns a clone of the conditions with associated selection of indexes.
        /// Called reclusively by BuildTree().
        /// </summary>
        private static bool BuildTree(ConditionOptimization optimization, EngineCore core, Transaction transaction, PhysicalIndexCatalog indexCatalog,
            PhysicalSchema physicalSchema, Conditions allConditions, string workingSchemaPrefix, SubCondition givenSubCondition)
        {
            foreach (var subConditionKey in givenSubCondition.ExpressionKeys)
            {
                var subCondition = allConditions.SubConditionFromKey(subConditionKey);

                if (subCondition.Conditions.Count > 0)
                {
                    if (subCondition.LogicalConnector == LogicalConnector.Or)
                    {
                        if (subCondition.Conditions.Any(o => o.Left.Prefix.Is(workingSchemaPrefix)) == false
                            && subCondition.Conditions.Any(o => o.Right.Prefix.Is(workingSchemaPrefix)) == false)
                        {
                            //Each "OR" condition group must have at least one potential indexable match for the selected schema,
                            //  this is because we need to be able to create a full list of all possible documents for this schema,
                            //  and if we have an "OR" group that does not further limit these documents by the given schema then
                            //  we will have to do a full namespace scan anyway.
                            return false; //Invalidate indexing optimization.
                        }
                    }

                    bool locatedGroupIndex = false;

                    //Loop through all indexes, all their attributes and all conditions in this sub-condition
                    //  for the given schema. Keep track of which indexes match each condition field.
                    foreach (var physicalIndex in indexCatalog.Collection)
                    {
                        Console.WriteLine($"Considering index: {physicalIndex.Name}");

                        bool locatedIndexAttribute = false;

                        var indexSelection = new IndexSelection(physicalIndex);
                        foreach (var attribute in physicalIndex.Attributes)
                        {
                            locatedIndexAttribute = false;
                            foreach (var condition in subCondition.Conditions.Where(o => o.Left.Prefix.Is(workingSchemaPrefix)))
                            {
                                if (condition.Left.Value?.Is(attribute.Field) == true)
                                {
                                    indexSelection.CoveredFields.Add(PrefixedField.Parse(condition.Left.Key));
                                    Console.WriteLine($"Indexed: {condition.ConditionKey} is ({condition.Left} {condition.LogicalQualifier} {condition.Right})");
                                    locatedIndexAttribute = true;
                                    locatedGroupIndex = true;
                                }
                            }

                            if (locatedIndexAttribute == false)
                            {
                                //We want to match the index attributes in the order which they appear in the index, so if we find 
                                //  one index attribute that we can not match to a condition then we need to break. In this case, we 
                                //  will either use a better index (if found) or use this partial index match with leaf-distillation.
                                break;
                            }
                        }

                        if (indexSelection.CoveredFields.Count > 0)
                        {
                            //We either have a full or a partial index match.
                            indexSelection.IsFullIndexMatch = indexSelection.CoveredFields.Count == indexSelection.Index.Attributes.Count;

                            subCondition.IndexSelections.Add(indexSelection);
                        }
                    }

                    if (locatedGroupIndex == false)
                    {
                        //This group has no indexing, but since it does reference the
                        //  given schema, we are going to have to do a full schema scan.
                        return false; //Invalidate indexing optimization.
                    }

                    foreach (var indexSelection in subCondition.IndexSelections)
                    {
                        Console.WriteLine($"{indexSelection.Index.Name}, CoveredFields: ({indexSelection.CoveredFields.Count})");
                    }
                }

                if (subCondition.ExpressionKeys.Count > 0)
                {
                    if (!BuildTree(optimization, core, transaction, indexCatalog, physicalSchema, allConditions, workingSchemaPrefix, subCondition))
                    {
                        return false; //Invalidate indexing optimization.
                    }
                }
            }

            return true;
        }

        #endregion

        public static List<Condition> GetConvertedConditions(List<Condition> conditions, List<PrefixedField> coveredFields)
        {
            var result = new List<Condition>();

            foreach (var coveredField in coveredFields)
            {
                foreach (var condition in conditions)
                {
                    if (condition.Left.Key == coveredField.Key)
                    {
                        result.Add(condition);
                    }
                }
            }

            return result;
        }

        #region Optimization explanation.

        private static string Pad(int indentation) => "".PadLeft(indentation * 2, ' ');

        /// <summary>
        /// This function makes a (somewhat) user readable expression tree, used for debugging and explanations.
        /// It includes indexes where they can be applied.
        /// It also demonstrates how we process the recursive condition logic.
        /// </summary>
        public string ExplainOptimization(int indentation = 0)
        {
            var result = new StringBuilder();

            if (Conditions.Root.ExpressionKeys.Count > 0)
            {
                //The root condition is just a pointer to a child condition, so get the "root" child condition.
                var rootCondition = Conditions.SubConditionFromKey(Conditions.Root.Key);
                ExplainSubCondition(ref result, rootCondition, indentation);
            }

            return result.ToString();
        }

        /// <summary>
        /// This function makes a (somewhat) user readable expression tree, used for debugging and explanations.
        /// It includes indexes where they can be applied.
        /// It also demonstrates how we process the recursive condition logic.
        /// Called by parent ExplainOptimization()
        /// </summary>
        private void ExplainSubCondition(ref StringBuilder result, SubCondition givenSubCondition, int indentation)
        {
            foreach (var subConditionKey in givenSubCondition.ExpressionKeys)
            {
                var subCondition = Conditions.SubConditionFromKey(subConditionKey);

                var indexName = subCondition.IndexSelection?.Index?.Name;

                result.AppendLine(Pad(indentation + 1)
                    + $"{Conditions.FriendlyPlaceholder(subCondition.Key)} is ({Conditions.FriendlyPlaceholder(subCondition.Expression)})"
                    + (indexName != null ? $" [{indexName}]" : ""));

                result.AppendLine(Pad(indentation + 1) + "(");

                if (subCondition.Conditions.Count > 0)
                {
                    foreach (var condition in subCondition.Conditions)
                    {
                        result.AppendLine(Pad(indentation + 2)
                            + $"{Conditions.FriendlyPlaceholder(condition.ConditionKey)} is ({condition.Left} {condition.LogicalQualifier} {condition.Right})");
                    }
                }

                if (subCondition.ExpressionKeys.Count > 0)
                {
                    result.AppendLine(Pad(indentation + 2) + "(");
                    ExplainSubCondition(ref result, subCondition, indentation + 2);
                    result.AppendLine(Pad(indentation + 2) + ")");
                }

                result.AppendLine(Pad(indentation + 1) + ")");
            }
        }

        #endregion
    }
}
