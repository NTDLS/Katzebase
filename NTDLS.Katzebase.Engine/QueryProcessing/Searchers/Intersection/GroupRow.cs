using NTDLS.Katzebase.Api.Types;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    /// <summary>
    /// Contains the template row for grouping operations (GroupRow)
    /// </summary>
    internal class GroupRow
    {
        /// <summary>
        /// Contains the template row for the group.
        /// </summary>
        public List<string?> Values { get; set; } = new();
        public KbInsensitiveDictionary<string?> OrderByValues { get; set; } = new();

        /// <summary>
        /// Pre-parsed numeric values for each ORDER BY field, keyed by field alias.
        /// Populated alongside <see cref="OrderByValues"/> at result-build time so the
        /// sort comparer can do a single numeric compare instead of calling TryParse
        /// on every comparison during the O(n log n) sort.  Null when the field value
        /// is not numeric.
        /// </summary>
        public KbInsensitiveDictionary<double?> OrderByNumericValues { get; set; } = new();

        /// <summary>
        /// Parameter values which are required to compute the aggregate functions for the group select fields.
        /// </summary>
        public KbInsensitiveDictionary<GroupAggregateFunctionParameter> SelectAggregateFunctionParameters { get; set; } = new();

        /// <summary>
        /// Parameter values which are required to compute the aggregate functions for the group order by.
        /// </summary>
        public KbInsensitiveDictionary<GroupAggregateFunctionParameter> SortAggregateFunctionParameters { get; set; } = new();
    }
}
