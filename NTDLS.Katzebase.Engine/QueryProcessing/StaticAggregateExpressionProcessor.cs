using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;

using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Query;
using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.QueryProcessing
{
    internal static class StaticAggregateExpressionProcessor
    {
        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static TData CollapseAggregateQueryField<TData>(this QueryField<TData> queryField, Transaction<TData> transaction,
            PreparedQuery<TData> query, KbInsensitiveDictionary<GroupAggregateFunctionParameter<TData>> aggregateFunctionParameters)
            where TData : IStringable
        {
            if (queryField.Expression is QueryFieldExpressionNumeric<TData> expressionNumeric)
            {
                return CollapseAggregateFunctionNumericParameter(transaction, query, expressionNumeric.FunctionDependencies, aggregateFunctionParameters, expressionNumeric.Value.ToT<string>());
            }
            else if (queryField.Expression is QueryFieldExpressionString<TData> expressionString)
            {
                return CollapseAggregateFunctionStringParameter(transaction, query, expressionString.FunctionDependencies, aggregateFunctionParameters, expressionString.Value.ToT<string>());
            }
            else
            {
                throw new KbEngineException($"Field expression type is not implemented: [{queryField.Expression.GetType().Name}].");
            }
        }

        /// <summary>
        /// Takes a string expression string and performs math on all of the values, including those from all
        ///     recursive function calls.
        /// </summary>
        private static TData CollapseAggregateFunctionNumericParameter<TData>(Transaction<TData> transaction, PreparedQuery<TData> query,
            List<IQueryFieldExpressionFunction> functions, KbInsensitiveDictionary<GroupAggregateFunctionParameter<TData>> aggregateFunctionParameters, string expressionString)
            where TData : IStringable
        {
            var tokenizer = new TokenizerSlim(expressionString, TokenizerExtensions.MathematicalCharacters);

            var token = tokenizer.EatGetNext();
            if (!tokenizer.IsExhausted())
            {
                throw new KbEngineException($"The aggregate function expression was not collapsed as expected..");
            }

            if (token.StartsWith("$x_") && token.EndsWith('$'))
            {
                //Search the dependency functions for the one with the expression key, this is the one we need to recursively resolve to fill in this token.
                var subFunction = functions.Single(o => o.ExpressionKey == token);
                return CollapseAggregateFunction(transaction, query, functions, aggregateFunctionParameters, subFunction);
            }

            //All aggregation parameters are collapsed as scaler expressions at query processing time.
            //There should never be anything to do here.
            throw new KbNotImplementedException();
        }

        /// <summary>
        /// Takes a string expression string and concatenates all of the values, including those from all
        ///     recursive function calls. Concatenation which is really the only operation we support for strings.
        /// </summary>
        private static TData CollapseAggregateFunctionStringParameter<TData>(Transaction<TData> transaction, PreparedQuery<TData> query,
            List<IQueryFieldExpressionFunction> functions, KbInsensitiveDictionary<GroupAggregateFunctionParameter<TData>> aggregateFunctionParameters, string expressionString)
            where TData : IStringable

        {
            var tokenizer = new TokenizerSlim(expressionString, ['+', '(', ')']);

            string token = tokenizer.EatGetNext();
            if (!tokenizer.IsExhausted())
            {
                throw new KbEngineException($"The aggregate function expression was not collapsed as expected..");
            }

            if (token.StartsWith("$x_") && token.EndsWith('$'))
            {
                //Search the dependency functions for the one with the expression key, this is the one we need to recursively resolve to fill in this token.
                var subFunction = functions.Single(o => o.ExpressionKey == token);
                return CollapseAggregateFunction(transaction, query, functions, aggregateFunctionParameters, subFunction);
            }

            //All aggregation parameters are collapsed as scaler expressions at query processing time.
            //There should never be anything to do here.
            throw new KbNotImplementedException();
        }

        /// <summary>
        /// Takes a function and recursively collapses all of the parameters, then recursively
        ///     executes all dependency functions to collapse the function to a single value.
        /// </summary>
        private static TData CollapseAggregateFunction<TData>(Transaction<TData> transaction, PreparedQuery<TData> query, List<IQueryFieldExpressionFunction> functions,
            KbInsensitiveDictionary<GroupAggregateFunctionParameter<TData>> aggregateFunctionParameters, IQueryFieldExpressionFunction function)
            where TData : IStringable
        {
            //The sole parameter for aggregate functions is pre-computed by the query execution engine, just get the values.
            if (aggregateFunctionParameters.TryGetValue(function.ExpressionKey, out var aggregateFunctionParameter) != true)
            {
                throw new KbEngineException($"The aggregate function [{function.FunctionName}] resolved expression key was not found: [{function.ExpressionKey}].");
            }

            return AggregateFunctionImplementation<TData>.ExecuteFunction(function.FunctionName, aggregateFunctionParameter);
        }
    }
}
