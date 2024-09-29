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
                "Numeric Avg (AggregationArray values)|'Returns the average value for the set.'",
                "Numeric Count (AggregationArray values)|'Returns the count of values in the set.'",
                "Numeric CountDistinct (AggregationArray values, boolean caseSensitive = false)|'Returns the count of distinct values in the set.'",
                "Numeric GeometricMean (AggregationArray values)|'Returns the average that is used to calculate the central tendency of a the set.'",
                "Numeric Max (AggregationArray values)|'Returns the maximum value for the set.'",
                "Numeric Mean (AggregationArray values)|'Returns the mean value for the set.'",
                "Numeric Median (AggregationArray values)|'Returns the median value for the set.'",
                "Numeric Min (AggregationArray values)|'Returns the minimum value for the set.'",
                "Numeric Mode (AggregationArray values)|'Returns the value that appears most frequently for the set.'",
                "Numeric Sum (AggregationArray values)|'Returns the total summative value for the set.'",
                "Numeric Variance (AggregationArray values)|'Returns a the measure of dispersion of a the set from their mean value.'",
                "String MinString (AggregationArray values)|'Returns the minimum string value for the set.'",
                "String MaxString (AggregationArray values)|'Returns the maximum string value for the set.'",
                "String Sha1Agg (AggregationArray values)|'Returns the SHA1 hash for the for the set.'",
                "String Sha256Agg (AggregationArray values)|'Returns the SHA256 hash for the for the set.'",
                "String Sha512Agg (AggregationArray values)|'Returns the SHA512 hash for the for the set.'",
            };

        public static string ExecuteFunction(string functionName, GroupAggregateFunctionParameter parameters)
        {
            var function = AggregateFunctionCollection.ApplyFunctionPrototype(functionName, parameters.SupplementalParameters);

            return functionName.ToLowerInvariant() switch
            {
                "avg" => AggregateAvg.Execute(parameters),
                "count" => AggregateCount.Execute(parameters),
                "countdistinct" => AggregateCountDistinct.Execute(function, parameters),
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

            throw new KbFunctionException($"Undefined function: [{functionName}].");
        }
    }
}
