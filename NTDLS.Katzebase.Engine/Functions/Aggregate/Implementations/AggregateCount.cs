using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateCount
    {
        public static string Execute(AggregateFunctionParameterValueCollection function, GroupAggregateFunctionParameter parameters)
        {
            if (function.Get<bool>("countDistinct"))
            {
                return parameters.AggregationValues.Distinct().Count().ToString();
            }
            return parameters.AggregationValues.Count().ToString();
        }
    }
}
