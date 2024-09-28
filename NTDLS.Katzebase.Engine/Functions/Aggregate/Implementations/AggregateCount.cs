using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateCount<TData> where TData : IStringable
    {
        public static string Execute(AggregateFunctionParameterValueCollection<TData> function, GroupAggregateFunctionParameter<TData> parameters)
        {
            if (function.Get<bool>("countDistinct"))
            {
                return parameters.AggregationValues.Distinct().Count().ToString();
            }
            return parameters.AggregationValues.Count().ToString();
        }
    }
}
