using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Parsers.Query;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using fs;
namespace NTDLS.Katzebase.Engine.QueryProcessing
{
    internal static class StaticAggregateExpressionProcessor
    {
        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static fstring CollapseAggregateQueryField(this QueryField queryField, Transaction transaction,
            PreparedQuery query, KbInsensitiveDictionary<GroupAggregateFunctionParameter> aggregateFunctionParameters)
        {
            if (queryField.Expression is QueryFieldExpressionNumeric expressionNumeric)
            {
                return CollapseAggregateFunctionNumericParameter(transaction, query, expressionNumeric.FunctionDependencies, aggregateFunctionParameters, expressionNumeric.Value.s);
            }
            else if (queryField.Expression is QueryFieldExpressionString expressionString)
            {
                return CollapseAggregateFunctionStringParameter(transaction, query, expressionString.FunctionDependencies, aggregateFunctionParameters, expressionString.Value.s);
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
        private static fstring CollapseAggregateFunctionNumericParameter(Transaction transaction, PreparedQuery query,
            List<IQueryFieldExpressionFunction> functions, KbInsensitiveDictionary<GroupAggregateFunctionParameter> aggregateFunctionParameters, string expressionString)
        {
            var tokenizer = new TokenizerSlim(expressionString, ['~', '!', '%', '^', '&', '*', '(', ')', '-', '/', '+']);

            var token = tokenizer.EatGetNext();
            if (!tokenizer.IsExausted())
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
        private static fstring CollapseAggregateFunctionStringParameter(Transaction transaction, PreparedQuery query,
            List<IQueryFieldExpressionFunction> functions, KbInsensitiveDictionary<GroupAggregateFunctionParameter> aggregateFunctionParameters, string expressionString)
        {
            var tokenizer = new TokenizerSlim(expressionString, ['+', '(', ')']);

            string token = tokenizer.EatGetNext();
            if (!tokenizer.IsExausted())
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
        private static fstring CollapseAggregateFunction(Transaction transaction, PreparedQuery query, List<IQueryFieldExpressionFunction> functions,
            KbInsensitiveDictionary<GroupAggregateFunctionParameter> aggregateFunctionParameters, IQueryFieldExpressionFunction function)
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
