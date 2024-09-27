using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateVariance<TData> where TData : IStringable
    {
        public static string Execute(GroupAggregateFunctionParameter<TData> parameters)
        {
            var numbers = parameters.AggregationValues.Select(o => o.ToT<double>()).ToList();
            double mean = numbers.Average();
            return (numbers.Sum(n => Math.Pow(n - mean, 2)) / numbers.Count).ToString();
        }
    }
}
