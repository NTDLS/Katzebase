using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateGeometricMean
    {
        public static string Execute(GroupAggregateFunctionParameter parameters)
        {
            var numbers = parameters.AggregationValues.Select(o => double.Parse(o)).ToList();
            double product = numbers.Aggregate(1.0, (acc, n) => acc * n);
            return (Math.Pow(product, 1.0 / numbers.Count)).ToString();
        }
    }
}
