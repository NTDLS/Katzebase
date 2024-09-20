using NTDLS.Katzebase.Engine.Parsers.Query.Fields;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions.Helpers
{
    internal static class ConditionExtensions
    {
        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of the group references. 
        /// </summary>
        public static List<ConditionGroup> FlattenToGroups(this List<ICondition> givenConditions)
        {
            var results = new List<ConditionGroup>();
            FlattenToGroups(givenConditions, results);
            return results;

            static void FlattenToGroups(List<ICondition> conditions, List<ConditionGroup> refGroups)
            {
                foreach (var condition in conditions)
                {
                    if (condition is ConditionGroup group)
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
        public static List<ConditionGroup> FlattenToGroups(this ConditionCollection givenConditions)
            => givenConditions.Collection.FlattenToGroups();

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of the entry references.
        /// </summary>
        public static List<ConditionEntry> FlattenToEntries(this List<ICondition> givenConditions)
        {
            var results = new List<ConditionEntry>();
            FlattenToEntries(givenConditions, results);
            return results;

            static void FlattenToEntries(List<ICondition> conditions, List<ConditionEntry> refEntries)
            {
                foreach (var condition in conditions)
                {
                    if (condition is ConditionGroup group)
                    {
                        FlattenToEntries(group.Collection, refEntries);
                    }
                    else if (condition is ConditionEntry entry)
                    {
                        refEntries.Add(entry);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of the entry references.
        /// </summary>
        public static List<ConditionEntry> FlattenToEntries(this ConditionCollection givenConditions)
            => givenConditions.Collection.FlattenToEntries();

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of left-hand-side document identifiers.
        /// </summary>
        public static List<QueryFieldDocumentIdentifier> FlattenToLeftDocumentIdentifiers(this List<ICondition> givenConditions)
        {
            var results = new List<QueryFieldDocumentIdentifier>();
            FlattenToLeftDocumentIdentifiers(givenConditions, results);
            return results;

            static void FlattenToLeftDocumentIdentifiers(List<ICondition> conditions, List<QueryFieldDocumentIdentifier> refConditions)
            {
                foreach (var condition in conditions)
                {
                    if (condition is ConditionGroup group)
                    {
                        FlattenToLeftDocumentIdentifiers(group.Collection, refConditions);
                    }
                    else if (condition is ConditionEntry entry)
                    {
                        if (entry.Left is QueryFieldDocumentIdentifier left)
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
        public static List<QueryFieldDocumentIdentifier> FlattenToLeftDocumentIdentifiers(this ConditionCollection givenConditions)
            => givenConditions.Collection.FlattenToLeftDocumentIdentifiers();

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of right-hand-side document identifiers.
        /// </summary>
        public static List<QueryFieldDocumentIdentifier> FlattenToRightDocumentIdentifiers(this List<ICondition> givenConditions)
        {
            var results = new List<QueryFieldDocumentIdentifier>();
            FlattenToRightDocumentIdentifiers(givenConditions, results);
            return results;

            static void FlattenToRightDocumentIdentifiers(List<ICondition> conditions, List<QueryFieldDocumentIdentifier> refConditions)
            {
                foreach (var condition in conditions)
                {
                    if (condition is ConditionGroup group)
                    {
                        FlattenToRightDocumentIdentifiers(group.Collection, refConditions);
                    }
                    else if (condition is ConditionEntry entry)
                    {
                        if (entry.Right is QueryFieldDocumentIdentifier right)
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
        public static List<QueryFieldDocumentIdentifier> FlattenToRightDocumentIdentifiers(this ConditionCollection givenConditions)
            => givenConditions.Collection.FlattenToRightDocumentIdentifiers();


        /// <summary>
        /// Rolls through a condition group, producing a flat list of the entry references where the left side is a document identifier.
        /// </summary>
        public static List<ConditionEntry> ThisLevelWithLeftDocumentIdentifiers(this ConditionGroup givenConditionGroups)
        {
            var results = new List<ConditionEntry>();

            foreach (var groupEntry in givenConditionGroups.Collection)
            {
                if (groupEntry is ConditionEntry entry)
                {
                    if (entry.Left is QueryFieldDocumentIdentifier)
                    {
                        results.Add(entry);
                    }
                }
                else if (groupEntry is ConditionGroup)
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
