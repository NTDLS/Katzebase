using fs;
namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    /// <summary>
    /// Contains the values that are needed to be stored at the group level, but per aggregate function expression key.
    /// For instance, when a query has two COUNT() function calls, there will be two instances of this class, each containing
    /// the values that need to be counted for each aggregate function expression.
    /// </summary>
    internal class GroupAggregateFunctionParameter
    {
        /// <summary>
        /// Contains the list of values that we will need to collapse aggregation functions.
        /// The key is the ExpressionKey of the aggregation function these values are for.
        /// </summary>
        public List<fstring> AggregationValues { get; set; } = new();

        /// <summary>
        /// List of aggregate function parameters after the default first "AggregationValues" parameter.
        /// The key is the ExpressionKey of the aggregation function these values are for.
        /// </summary>
        public List<fstring?> SupplementalParameters { get; set; } = new();
    }
}
