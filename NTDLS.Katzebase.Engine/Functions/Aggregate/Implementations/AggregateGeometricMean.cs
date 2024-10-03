using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    public static class AggregateGeometricMean<TData> where TData : IStringable
    {
        public static string Execute(GroupAggregateFunctionParameter<TData> parameters)
        {
            var numbers = parameters.AggregationValues.Select(o => o.ToT<double>()).ToList();
            double product = numbers.Aggregate(1.0, (acc, n) => acc * n);
            return (Math.Pow(product, 1.0 / numbers.Count)).ToString();
        }
    }
}
