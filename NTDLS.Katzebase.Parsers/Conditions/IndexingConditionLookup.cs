﻿namespace NTDLS.Katzebase.Parsers.Conditions
{
    /// <summary>
    /// Contains a list of conditions and the index which is to be used for matching them.
    /// </summary>
    public class IndexingConditionLookup(IndexSelection indexSelection)
    {
        public IndexSelection IndexSelection { get; set; } = indexSelection;

        /*
        For an index which is on LastName, FirstName, the conditions could look like this

        Conditions["LastName"] = { "Doe" }
        Conditions["FirstName"] = { "Jane", "John" }
        */
        /// <summary>
        /// Dictionary of index attribute field name that contains the conditions that need to be matched on that index attribute level.
        /// </summary>
        public Dictionary<string, List<ConditionEntry>> AttributeConditionSets { get; set; } = new();
    }
}
