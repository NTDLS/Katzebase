using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    /// <summary>
    /// Contains the template row for grouping operations (GroupRow)
    /// </summary>
    internal class GroupRowCollection
    {
        /// <summary>
        /// Contains the template row for the group.
        /// </summary>
        public List<string?> GroupRow { get; set; } = new();

        /// <summary>
        /// Contains the list of values that we will need to collapse aggregation functions.
        /// The key is the ExpressionKey of the aggregation function these values are for.
        /// </summary>
        public KbInsensitiveDictionary<List<string>> AggregationValues { get; set; } = new();

        /// <summary>
        /// List of aggregate function parameters after the default first "AggregationValues" parameter.
        /// The key is the ExpressionKey of the aggregation function these values are for.
        /// </summary>
        public KbInsensitiveDictionary<List<string>> SupplementalParameters { get; set; } = new();
    }
}
