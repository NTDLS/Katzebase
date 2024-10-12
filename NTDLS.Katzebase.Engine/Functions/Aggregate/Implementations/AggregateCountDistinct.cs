using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateCountDistinct
    {
        public static string? Execute(AggregateFunctionParameterValueCollection function, GroupAggregateFunctionParameter parameters)
        {
            if (function.Get<bool?>("caseSensitive") == true)
            {
                return parameters.AggregationValues.Distinct(StringComparer.InvariantCultureIgnoreCase).Count().ToString();
            }
            return parameters.AggregationValues.Distinct().Count().ToString();
        }
    }
}
