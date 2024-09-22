using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateMinString
    {
        public static string Execute(GroupAggregateFunctionParameter parameters)
        {
            return parameters.AggregationValues.OrderBy(o => o).First().ToString();
        }
    }
}
