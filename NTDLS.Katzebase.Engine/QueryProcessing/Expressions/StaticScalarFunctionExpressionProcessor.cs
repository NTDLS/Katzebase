using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Query.Functions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;


namespace NTDLS.Katzebase.Engine.QueryProcessing.Expressions
{
    internal static class StaticScalarFunctionExpressionProcessor
    {
        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static string? CollapseScalarExpressionFunctionParameter(this IExpressionFunctionParameter parameter, Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<string?> auxiliaryFields, List<IQueryFieldExpressionFunction> functionDependencies)
        {
            if (parameter is ExpressionFunctionParameterString parameterString)
            {
                return StaticScalarExpressionProcessor.CollapseScalarStringExpression(transaction, query, fieldCollection, auxiliaryFields, functionDependencies, parameterString.Expression);
            }
            else if (parameter is ExpressionFunctionParameterNumeric parameterNumeric)
            {
                return StaticScalarExpressionProcessor.CollapseScalarNumericExpression(transaction, query, fieldCollection, auxiliaryFields, functionDependencies, parameterNumeric.Expression);
            }
            else if (parameter is ExpressionFunctionParameterFunction expressionFunctionParameterFunction)
            {
                return StaticScalarExpressionProcessor.CollapseScalarNumericExpression(transaction, query, fieldCollection, auxiliaryFields, functionDependencies, expressionFunctionParameterFunction.Expression);
            }
            else
            {
                throw new KbNotImplementedException($"Function parameter type is not implemented [{parameter.GetType().Name}].");
            }
        }

        /// <summary>
        /// Takes a function and recursively collapses all of the parameters, then recursively
        ///     executes all dependency functions to collapse the function to a single value.
        /// </summary>
        internal static string? CollapseScalarFunction(Transaction transaction, PreparedQuery query, QueryFieldCollection fieldCollection,
            KbInsensitiveDictionary<string?> auxiliaryFields, List<IQueryFieldExpressionFunction> functions, IQueryFieldExpressionFunction function)
        {
            var collapsedParameters = new List<string?>();

            foreach (var parameter in function.Parameters)
            {
                collapsedParameters.Add(parameter.CollapseScalarExpressionFunctionParameter(transaction, query, fieldCollection, auxiliaryFields, functions));
            }

            if (AggregateFunctionCollection.TryGetFunction(function.FunctionName, out _))
            {
                throw new KbProcessingException($"Cannot perform scalar operation on aggregate result of: [{function.FunctionName}].");
            }

            //Execute function with the parameters from above ↑
            var methodResult = ScalarFunctionImplementation.ExecuteFunction(transaction, function.FunctionName, collapsedParameters, auxiliaryFields);

            return methodResult;
        }
    }
}
