using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateSum
    {
        public static string Execute(GroupAggregateFunctionParameter parameters)
        {
            return parameters.AggregationValues.Sum(o => double.Parse(o)).ToString();
        }
    }
}
