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
                        FlattenToGroups(group.Entries, refGroups);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of the group references. 
        /// </summary>
        public static List<ConditionGroup> FlattenToGroups(this ConditionCollection givenConditions)
            => givenConditions.Entries.FlattenToGroups();

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
                        FlattenToEntries(group.Entries, refEntries);
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
            => givenConditions.Entries.FlattenToEntries();

        /// <summary>
        /// Recursively rolls through nested conditions, producing a flat list of the entry references where the left side is a document identifier.
        /// </summary>
        public static List<ConditionEntry> FlattenToDocumentIdentifiers(this List<ICondition> givenConditions)
        {
            var results = new List<ConditionEntry>();
            FlattenToDocumentIdentifiers(givenConditions, results);
            return results;

            static void FlattenToDocumentIdentifiers(List<ICondition> conditions, List<ConditionEntry> refConditions)
            {
                foreach (var condition in conditions)
                {
                    if (condition is ConditionGroup group)
                    {
                        FlattenToDocumentIdentifiers(group.Entries, refConditions);
                    }
                    else if (condition is ConditionEntry entry)
                    {
                        if (entry.Left is QueryFieldDocumentIdentifier)
                        {
                            refConditions.Add(entry);
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
        /// Recursively rolls through nested conditions, producing a flat list of the entry references where the left side is a document identifier.
        /// </summary>
        public static List<ConditionEntry> FlattenToDocumentIdentifiers(this ConditionCollection givenConditions)
            => givenConditions.Entries.FlattenToDocumentIdentifiers();

        /// <summary>
        /// Rolls through a condition group, producing a flat list of the entry references where the left side is a document identifier.
        /// </summary>
        public static List<ConditionEntry> FlattenToDocumentIdentifiers(this ConditionGroup givenConditionGroups)
        {
            var results = new List<ConditionEntry>();

            foreach (var groupEntry in givenConditionGroups.Entries)
            {
                if (groupEntry is ConditionEntry entry)
                {
                    if (entry.Left is QueryFieldDocumentIdentifier)
                    {
                        results.Add(entry);
                    }
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
