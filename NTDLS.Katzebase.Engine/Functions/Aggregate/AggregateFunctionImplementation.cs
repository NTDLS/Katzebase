using fs;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    /// <summary>
    /// Contains all function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    internal class AggregateFunctionImplementation
    {
        internal static string[] PrototypeStrings = {
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

        public static fstring ExecuteFunction(string functionName, GroupAggregateFunctionParameter parameters)
        {
            var function = AggregateFunctionCollection.ApplyFunctionPrototype(functionName, parameters.SupplementalParameters);

            var rtn = functionName.ToLowerInvariant() switch
            {
                "avg" => AggregateAvg.Execute(parameters),
                "count" => AggregateCount.Execute(function, parameters),
                "geometricmean" => AggregateGeometricMean.Execute(parameters),
                "max" => AggregateMax.Execute(parameters),
                "mean" => AggregateMean.Execute(parameters),
                "median" => AggregateMedian.Execute(parameters),
                "min" => AggregateMin.Execute(parameters),
                "mode" => AggregateMode.Execute(parameters),
                "sum" => AggregateSum.Execute(parameters),
                "variance" => AggregateVariance.Execute(parameters),
                "minstring" => AggregateMinString.Execute(parameters),
                "maxstring" => AggregateMaxString.Execute(parameters),
                "sha1agg" => AggregateSha1Agg.Execute(parameters),
                "sha256agg" => AggregateSha256Agg.Execute(parameters),
                "sha512agg" => AggregateSha512Agg.Execute(parameters),

                _ => throw new KbParserException($"The aggregate function is not implemented: [{functionName}].")
            };

            return fstring.NewS(rtn);

            //throw new KbFunctionException($"Undefined function: {functionName}.");
        }
    }
}
