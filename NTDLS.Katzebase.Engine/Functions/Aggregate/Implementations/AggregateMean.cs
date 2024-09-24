using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateMean
    {
        public static string Execute(GroupAggregateFunctionParameter parameters)
        {
            return parameters.AggregationValues.Average(o => double.Parse(o.s)).ToString();
        }
    }
}
