using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Expressions
{
    internal static class StaticAggregateFunctionExpressionExtensions
    {
        /// <summary>
        /// Takes a function and recursively collapses all of the parameters, then recursively
        ///     executes all dependency functions to collapse the function to a single value.
        /// </summary>
        public static string? CollapseAggregateFunction(this IQueryFieldExpressionFunction function,
            KbInsensitiveDictionary<GroupAggregateFunctionParameter> aggregateFunctionParameters)
        {
            //The sole parameter for aggregate functions is pre-computed by the query execution engine, just get the values.
            if (aggregateFunctionParameters.TryGetValue(function.ExpressionKey, out var aggregateFunctionParameter) != true)
            {
                throw new KbEngineException($"The aggregate function [{function.FunctionName}] resolved expression key was not found: [{function.ExpressionKey}].");
            }

            return AggregateFunctionImplementation.ExecuteFunction(function.FunctionName, aggregateFunctionParameter);
        }
    }
}
