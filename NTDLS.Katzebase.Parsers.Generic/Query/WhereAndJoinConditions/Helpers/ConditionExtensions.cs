using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions.Helpers
{
    public static class ConditionExtensions
    {
        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of the group references. 
        /// </summary>
        public static List<ConditionGroup<TData>> FlattenToGroups<TData>(this List<ICondition> givenConditions) where TData : IStringable
        {
            var results = new List<ConditionGroup<TData>>();
            FlattenToGroups(givenConditions, results);
            return results;

            static void FlattenToGroups(List<ICondition> conditions, List<ConditionGroup<TData>> refGroups)
            {
                foreach (var condition in conditions)
                {
                    if (condition is ConditionGroup<TData> group)
                    {
                        refGroups.Add(group);
                        FlattenToGroups(group.Collection, refGroups);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of the group references. 
        /// </summary>
        public static List<ConditionGroup<TData>> FlattenToGroups<TData>(this ConditionCollection<TData> givenConditions) where TData : IStringable
            => givenConditions.Collection.FlattenToGroups<TData>();

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of the entry references.
        /// </summary>
        public static List<ConditionEntry<TData>> FlattenToEntries<TData>(this List<ICondition> givenConditions) where TData : IStringable
        {
            var results = new List<ConditionEntry<TData>>();
            FlattenToEntries(givenConditions, results);
            return results;

            static void FlattenToEntries(List<ICondition> conditions, List<ConditionEntry<TData>> refEntries)
            {
                foreach (var condition in conditions)
                {
                    if (condition is ConditionGroup<TData> group)
                    {
                        FlattenToEntries(group.Collection, refEntries);
                    }
                    else if (condition is ConditionEntry<TData> entry)
                    {
                        refEntries.Add(entry);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of the entry references.
        /// </summary>
        public static List<ConditionEntry<TData>> FlattenToEntries<TData>(this ConditionCollection<TData> givenConditions) where TData : IStringable
            => givenConditions.Collection.FlattenToEntries<TData>();

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of left-hand-side document identifiers.
        /// </summary>
        public static List<QueryFieldDocumentIdentifier<TData>> FlattenToLeftDocumentIdentifiers<TData>(this List<ICondition> givenConditions) where TData : IStringable
        {
            var results = new List<QueryFieldDocumentIdentifier<TData>>();
            FlattenToLeftDocumentIdentifiers(givenConditions, results);
            return results;

            static void FlattenToLeftDocumentIdentifiers(List<ICondition> conditions, List<QueryFieldDocumentIdentifier<TData>> refConditions)
            {
                foreach (var condition in conditions)
                {
                    if (condition is ConditionGroup<TData> group)
                    {
                        FlattenToLeftDocumentIdentifiers(group.Collection, refConditions);
                    }
                    else if (condition is ConditionEntry<TData> entry)
                    {
                        if (entry.Left is QueryFieldDocumentIdentifier<TData> left)
                        {
                            refConditions.Add(left);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of left-hand-side document identifiers.
        /// </summary>
        public static List<QueryFieldDocumentIdentifier<TData>> FlattenToLeftDocumentIdentifiers<TData>(this ConditionCollection<TData> givenConditions) where TData : IStringable
            => givenConditions.Collection.FlattenToLeftDocumentIdentifiers<TData>();

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of right-hand-side document identifiers.
        /// </summary>
        public static List<QueryFieldDocumentIdentifier<TData>> FlattenToRightDocumentIdentifiers<TData>(this List<ICondition> givenConditions) where TData : IStringable
        {
            var results = new List<QueryFieldDocumentIdentifier<TData>>();
            FlattenToRightDocumentIdentifiers(givenConditions, results);
            return results;

            static void FlattenToRightDocumentIdentifiers(List<ICondition> conditions, List<QueryFieldDocumentIdentifier<TData>> refConditions)
            {
                foreach (var condition in conditions)
                {
                    if (condition is ConditionGroup<TData> group)
                    {
                        FlattenToRightDocumentIdentifiers(group.Collection, refConditions);
                    }
                    else if (condition is ConditionEntry<TData> entry)
                    {
                        if (entry.Right is QueryFieldDocumentIdentifier<TData> right)
                        {
                            refConditions.Add(right);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of right-hand-side document identifiers.
        /// </summary>
        public static List<QueryFieldDocumentIdentifier<TData>> FlattenToRightDocumentIdentifiers<TData>(this ConditionCollection<TData> givenConditions) where TData : IStringable
            => givenConditions.Collection.FlattenToRightDocumentIdentifiers<TData>();


        /// <summary>
        /// Rolls through a condition group, producing a flat list of the entry references where the left side is a document identifier.
        /// </summary>
        public static List<ConditionEntry<TData>> ThisLevelWithLeftDocumentIdentifiers<TData>(this ConditionGroup<TData> givenConditionGroups) where TData : IStringable
        {
            var results = new List<ConditionEntry<TData>>();

            foreach (var groupEntry in givenConditionGroups.Collection)
            {
                if (groupEntry is ConditionEntry<TData> entry)
                {
                    if (entry.Left is QueryFieldDocumentIdentifier<TData>)
                    {
                        results.Add(entry);
                    }
                }
                else if (groupEntry is ConditionGroup<TData>)
                {
                    //We only get the current level, so ignore these.
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return results;
        }
    }
}
