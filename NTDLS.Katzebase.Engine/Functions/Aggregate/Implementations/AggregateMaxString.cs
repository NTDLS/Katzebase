using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    public static class AggregateMaxString<TData> where TData : IStringable
    {
        public static string Execute(GroupAggregateFunctionParameter<TData> parameters)
        {
            return parameters.AggregationValues.OrderByDescending(o => o).First().ToString();
        }
    }
}
