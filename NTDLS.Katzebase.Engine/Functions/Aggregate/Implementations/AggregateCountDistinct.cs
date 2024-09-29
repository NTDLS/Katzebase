using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateCountDistinct
    {
        public static string Execute(AggregateFunctionParameterValueCollection function, GroupAggregateFunctionParameter parameters)
        {
            if (function.Get<bool>("caseSensitive"))
            {
                return parameters.AggregationValues.Distinct(StringComparer.InvariantCultureIgnoreCase).Count().ToString();
            }
            return parameters.AggregationValues.Distinct().Count().ToString();
        }
    }
}
