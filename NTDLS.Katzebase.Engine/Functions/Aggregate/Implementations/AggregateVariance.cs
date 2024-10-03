using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    public static class AggregateVariance<TData> where TData : IStringable
    {
        public static string Execute(GroupAggregateFunctionParameter<TData> parameters)
        {
            var numbers = parameters.AggregationValues.Select(o => o.ToT<double>()).ToList();
            double mean = numbers.Average();
            return (numbers.Sum(n => Math.Pow(n - mean, 2)) / numbers.Count).ToString();
        }
    }
}
