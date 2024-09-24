using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateMax
    {
        public static string Execute(GroupAggregateFunctionParameter parameters)
        {
            return parameters.AggregationValues.Max(o => double.Parse(o.s)).ToString();
        }
    }
}
