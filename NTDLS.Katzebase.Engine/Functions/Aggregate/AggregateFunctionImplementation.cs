using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations;
namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    /// <summary>
    /// Contains all function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    internal class AggregateFunctionImplementation<TData> where TData : IStringable
    {
        public static string[] PrototypeStrings = {
                //Prototype Format: "returnDataType functionName (parameterDataType parameterName, parameterDataType parameterName = defaultValue)"
                //Parameters that have a default specified are considered optional, they should come after non-optional parameters.
                "Numeric Avg (AggregationArray values)",
                "Numeric Count (AggregationArray values, boolean countDistinct = false)",
                "Numeric GeometricMean (AggregationArray values)",
                "Numeric Max (AggregationArray values)",
                "Numeric Mean (AggregationArray values)",
                "Numeric Median (AggregationArray values)",
                "Numeric Min (AggregationArray values)",
                "Numeric Mode (AggregationArray values)",
                "Numeric Sum (AggregationArray values)",
                "Numeric Variance (AggregationArray values)",
                "String MinString (AggregationArray values)",
                "String MaxString (AggregationArray values)",
                "String Sha1Agg (AggregationArray values)",
                "String Sha256Agg (AggregationArray values)",
                "String Sha512Agg (AggregationArray values)",
            };

        public static TData ExecuteFunction(string functionName, GroupAggregateFunctionParameter<TData> parameters)
        {
            var function = AggregateFunctionCollection<TData>.ApplyFunctionPrototype(functionName, parameters.SupplementalParameters);

            var rtn = functionName.ToLowerInvariant() switch
            {
                "avg" => AggregateAvg<TData>.Execute(parameters),
                "count" => AggregateCount<TData>.Execute(function, parameters),
                "geometricmean" => AggregateGeometricMean<TData>.Execute(parameters),
                "max" => AggregateMax<TData>.Execute(parameters),
                "mean" => AggregateMean<TData>.Execute(parameters),
                "median" => AggregateMedian<TData>.Execute(parameters),
                "min" => AggregateMin<TData>.Execute(parameters),
                "mode" => AggregateMode<TData>.Execute(parameters),
                "sum" => AggregateSum<TData>.Execute(parameters),
                "variance" => AggregateVariance<TData>.Execute(parameters),
                "minstring" => AggregateMinString<TData>.Execute(parameters),
                "maxstring" => AggregateMaxString<TData>.Execute(parameters),
                "sha1agg" => AggregateSha1Agg<TData>.Execute(parameters),
                "sha256agg" => AggregateSha256Agg<TData>.Execute(parameters),
                "sha512agg" => AggregateSha512Agg<TData>.Execute(parameters),

                _ => throw new KbParserException($"The aggregate function is not implemented: [{functionName}].")
            };

            return rtn.CastToT<TData>(EngineCore<TData>.StrCast);
            throw new KbFunctionException($"Undefined function: [{functionName}].");
        }
    }
}
