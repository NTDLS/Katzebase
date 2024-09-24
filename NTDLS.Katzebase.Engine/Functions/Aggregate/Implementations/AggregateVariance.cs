using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateVariance
    {
        public static string Execute(GroupAggregateFunctionParameter parameters)
        {
            var numbers = parameters.AggregationValues.Select(o => double.Parse(o.s)).ToList();
            double mean = numbers.Average();
            return (numbers.Sum(n => Math.Pow(n - mean, 2)) / numbers.Count).ToString();
        }
    }
}
