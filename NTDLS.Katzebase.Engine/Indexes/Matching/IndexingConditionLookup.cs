using NTDLS.Katzebase.Engine.Query.Constraints;

namespace NTDLS.Katzebase.Engine.Indexes.Matching
{
    /// <summary>
    /// Contains a list of conditions and the index which is to be used for matching them.
    /// </summary>
    internal class IndexingConditionLookup
    {
        public PhysicalIndex Index { get; set; }

        /*
        For an index which is on LastName, FirstName, the conditions could look like this

        Conditions["LastName"] = { "Doe" }
        Conditions["FirstName"] = { "Jane", "John" }
        */
        /// <summary>
        /// Dictionary of index attribute field name that contains the conditions that need to be matched on that index attribute level.
        /// </summary>
        public Dictionary<string, List<Condition>> AttributeConditionSets { get; set; } = new();

        public IndexingConditionLookup(PhysicalIndex index)
        {
            Index = index;
        }
    }
}
