using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateMaxString
    {
        public static string Execute(GroupAggregateFunctionParameter parameters)
        {
            return parameters.AggregationValues.OrderByDescending(o => o).First().ToString();
        }
    }
}
