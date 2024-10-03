using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    public static class AggregateMinString<TData> where TData : IStringable
    {
        public static string Execute(GroupAggregateFunctionParameter<TData> parameters)
        {
            return parameters.AggregationValues.OrderBy(o => o).First().ToString();
        }
    }
}
