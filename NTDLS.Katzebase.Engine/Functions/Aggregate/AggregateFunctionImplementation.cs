using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Functions.Aggregate.Implementations;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    /// <summary>
    /// Contains all function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    internal class AggregateFunctionImplementation
    {
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

                _ => throw new KbFunctionException($"The aggregate function is not implemented: [{functionName}].")
            };

            throw new KbFunctionException($"Undefined function: [{functionName}].");
        }
    }
}
