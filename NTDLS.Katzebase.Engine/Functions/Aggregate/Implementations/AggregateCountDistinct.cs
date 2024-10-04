using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Interfaces;
using System.Linq;
namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations
{
    internal static class AggregateCountDistinct
    {
        public static string Execute<TData>(AggregateFunctionParameterValueCollection<TData> function, GroupAggregateFunctionParameter<TData> parameters) where TData : IStringable
        {
            if (function.Get<bool>("caseSensitive"))
            {
                return parameters.AggregationValues.Distinct(EngineCore<TData>.Comparer).Count().ToString();
            }
            return parameters.AggregationValues.Distinct().Count().ToString();
        }
    }
}
