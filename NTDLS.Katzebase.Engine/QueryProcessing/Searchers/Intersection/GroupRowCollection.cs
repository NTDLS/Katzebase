using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
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
    }
}
