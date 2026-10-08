using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateCountDistinct
    {
        public static string? Execute(AggregateFunctionParameterValueCollection function, GroupAggregateFunctionParameter parameters)
        {
            var comparer = function.Get<bool?>("caseSensitive") == true
                ? StringComparer.InvariantCulture
                : StringComparer.InvariantCultureIgnoreCase;

            return parameters.AggregationValues.Distinct(comparer).Count().ToString();
        }
    }
}
