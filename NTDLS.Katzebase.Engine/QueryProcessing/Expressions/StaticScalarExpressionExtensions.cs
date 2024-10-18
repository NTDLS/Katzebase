using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Query.Functions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Expressions
{
    internal static class StaticScalarExpressionExtensions
    {
        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static string? CollapseScalarExpression(this IExpressionFunctionParameter parameter, Transaction transaction, PreparedQuery query,
            QueryFieldCollection fieldCollection, KbInsensitiveDictionary<string?> auxiliaryFields, List<IQueryFieldExpressionFunction> functionDependencies,
            KbInsensitiveDictionary<GroupAggregateFunctionParameter>? aggregateFunctionParameters = null)
        {
            if (parameter is ExpressionFunctionParameterString parameterString)
            {
                return StaticScalarExpressionProcessor.CollapseScalarStringExpression(transaction, query, fieldCollection,
                    auxiliaryFields, functionDependencies, parameterString.Expression, aggregateFunctionParameters);
            }
            else if (parameter is ExpressionFunctionParameterNumeric parameterNumeric)
            {
                return StaticScalarExpressionProcessor.CollapseScalarNumericExpression(transaction, query, fieldCollection,
                    auxiliaryFields, functionDependencies, parameterNumeric.Expression, aggregateFunctionParameters);
            }
            else if (parameter is ExpressionFunctionParameterFunction expressionFunctionParameterFunction)
            {
                return StaticScalarExpressionProcessor.CollapseScalarNumericExpression(transaction, query, fieldCollection,
                    auxiliaryFields, functionDependencies, expressionFunctionParameterFunction.Expression, aggregateFunctionParameters);
            }
            else
            {
                throw new KbNotImplementedException($"Function parameter type is not implemented [{parameter.GetType().Name}].");
            }
        }
    }
}
