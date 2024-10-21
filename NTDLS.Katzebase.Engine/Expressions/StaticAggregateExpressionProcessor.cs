using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers;
using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Tokens;


namespace NTDLS.Katzebase.Engine.Expressions
{
    internal static class StaticAggregateExpressionProcessor
    {
        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static string? CollapseAggregateQueryField(this QueryField queryField, Transaction transaction,
            PreparedQuery query, KbInsensitiveDictionary<GroupAggregateFunctionParameter> aggregateFunctionParameters)
        {
            if (queryField.Expression is QueryFieldExpressionNumeric expressionNumeric)
            {
                return CollapseAggregateFunctionNumericParameter(transaction, query,
                    expressionNumeric.FunctionDependencies, aggregateFunctionParameters, expressionNumeric.Value.EnsureNotNull());
            }
            else if (queryField.Expression is QueryFieldExpressionString expressionString)
            {
                return CollapseAggregateFunctionStringParameter(transaction, query,
                    expressionString.FunctionDependencies, aggregateFunctionParameters, expressionString.Value.EnsureNotNull());
            }
            else
            {
                throw new KbNotImplementedException($"Field expression type is not implemented: [{queryField.Expression.GetType().Name}].");
            }
        }

        /// <summary>
        /// Takes a string expression string and performs math on all of the values, including those from all
        ///     recursive function calls.
        /// </summary>
        private static string? CollapseAggregateFunctionNumericParameter(Transaction transaction,
            PreparedQuery query, List<IQueryFieldExpressionFunction> functions,
            KbInsensitiveDictionary<GroupAggregateFunctionParameter> aggregateFunctionParameters, string expressionString)
        {
            var tokenizer = new TokenizerSlim(expressionString, TokenizerExtensions.MathematicalCharacters);

            var token = tokenizer.EatGetNext();
            if (!tokenizer.IsExhausted())
            {
                throw new KbProcessingException($"The aggregate function expression was not collapsed as expected.");
            }

            if (token.StartsWith("$x_") && token.EndsWith('$'))
            {
                //Search the dependency functions for the one with the expression key, this is the one we need to recursively resolve to fill in this token.
                var subFunction = functions.Single(o => o.ExpressionKey == token);

                if (AggregateFunctionCollection.TryGetFunction(subFunction.FunctionName, out var aggregateFunction))
                {
                    return subFunction.CollapseAggregateFunction(aggregateFunctionParameters);
                }
                else if (ScalarFunctionCollection.TryGetFunction(subFunction.FunctionName, out var scalarFunction))
                {
                    //TODO: We either need to create a new version of this or pass it optional aggregateFunctionParameters. 
                    return subFunction.CollapseScalarFunction(transaction, query,
                        new QueryFieldCollection(query.Batch), new KbInsensitiveDictionary<string?>(), functions, aggregateFunctionParameters);
                }
            }

            //All aggregation parameters are collapsed as scalar expressions at query processing time.
            //There should never be anything to do here.
            throw new KbNotImplementedException();
        }

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

        /// <summary>
        /// Takes a string expression string and concatenates all of the values, including those from all
        ///     recursive function calls. Concatenation which is really the only operation we support for strings.
        /// </summary>
        private static string? CollapseAggregateFunctionStringParameter(Transaction transaction, PreparedQuery query, List<IQueryFieldExpressionFunction> functions,
            KbInsensitiveDictionary<GroupAggregateFunctionParameter> aggregateFunctionParameters, string expressionString)
        {
            var tokenizer = new TokenizerSlim(expressionString, ['+', '(', ')']);

            var token = tokenizer.EatGetNext();
            if (!tokenizer.IsExhausted())
            {
                throw new KbProcessingException($"The aggregate function expression was not collapsed as expected.");
            }

            if (token.StartsWith("$x_") && token.EndsWith('$'))
            {
                //Search the dependency functions for the one with the expression key, this is the one we need to recursively resolve to fill in this token.
                var subFunction = functions.Single(o => o.ExpressionKey == token);

                if (AggregateFunctionCollection.TryGetFunction(subFunction.FunctionName, out var aggregateFunction))
                {
                    return subFunction.CollapseAggregateFunction(aggregateFunctionParameters);
                }
                else if (ScalarFunctionCollection.TryGetFunction(subFunction.FunctionName, out var scalarFunction))
                {
                    return subFunction.CollapseScalarFunction(transaction, query,
                        new QueryFieldCollection(query.Batch), new KbInsensitiveDictionary<string?>(), functions, aggregateFunctionParameters);
                }
                else
                {
                    throw new KbEngineException($"Unknown function type: [{subFunction.FunctionName}].");
                }
            }

            //All aggregation parameters are collapsed as scalar expressions at query processing time.
            //There should never be anything to do here.
            throw new KbNotImplementedException();
        }
    }
}
