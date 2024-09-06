using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Parsers.Query;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Engine.Parsers.Query.Functions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using System.Text;

namespace NTDLS.Katzebase.Engine.Query
{
    internal class StaticAggregateExpressionProcessor
    {
        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static string CollapseAggregateQueryField(Transaction transaction,
            PreparedQuery query, KbInsensitiveDictionary<List<string>> aggregationValues,
            KbInsensitiveDictionary<string?> auxiliaryFields, //TODO: we need to add this to the group ro so we can pass it in.
            QueryField queryField)
        {
            if (queryField.Expression is QueryFieldExpressionNumeric expressionNumeric)
            {
                return CollapseAggregateFunctionNumericParameter(transaction, query, aggregationValues, auxiliaryFields, expressionNumeric.FunctionDependencies, expressionNumeric.Value);
            }
            else if (queryField.Expression is QueryFieldExpressionString expressionString)
            {
                return CollapseAggregateFunctionStringParameter(transaction, query, aggregationValues, auxiliaryFields, expressionString.FunctionDependencies, expressionString.Value);
            }
            else if (queryField.Expression is QueryFieldDocumentIdentifier documentIdentifier)
            {
                if (auxiliaryFields.TryGetValue(documentIdentifier.Value, out var auxiliaryValue))
                {
                    return auxiliaryValue ?? string.Empty; //TODO: Should auxiliaryFields really allow NULL values?
                }
                throw new KbEngineException($"Auxiliary fields not found: [{documentIdentifier.Value}].");
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
        static string CollapseAggregateFunctionNumericParameter(Transaction transaction, PreparedQuery query,
            KbInsensitiveDictionary<List<string>> aggregationValues, KbInsensitiveDictionary<string?> auxiliaryFields,
            List<IQueryFieldExpressionFunction> functions, string expressionString)
        {
            //Build a cachable numeric expression, interpolate the values and execute the expression.

            var tokenizer = new TokenizerSlim(expressionString, ['~', '!', '%', '^', '&', '*', '(', ')', '-', '/', '+']);

            int variableNumber = 0;

            var expressionVariables = new Dictionary<string, double>();

            while (!tokenizer.Exausted())
            {
                var token = tokenizer.EatGetNext();

                if (token.StartsWith("$f_") && token.EndsWith('$'))
                {
                    //Resolve the token to a field identifier.
                    if (query.SelectFields.DocumentIdentifiers.TryGetValue(token, out var fieldIdentifier))
                    {
                        //Resolve the field identifier to a value.
                        if (auxiliaryFields.TryGetValue(fieldIdentifier.Value, out var textValue))
                        {
                            textValue.EnsureNotNull();
                            string mathVariable = $"v{variableNumber++}";
                            expressionString = expressionString.Replace(token, mathVariable);
                            expressionVariables.Add(mathVariable, double.Parse(query.Batch.GetLiteralValue(textValue)));
                        }
                        else
                        {
                            throw new KbEngineException($"Function parameter auxiliary field is not defined: [{token}].");
                        }
                    }
                    else
                    {
                        throw new KbEngineException($"Function parameter field is not defined: [{token}].");
                    }
                }
                else if (token.StartsWith("$x_") && token.EndsWith('$'))
                {
                    //Search the dependency functions for the one with the expression key, this is the one we need to recursively resolve to fill in this token.
                    var subFunction = functions.Single(o => o.ExpressionKey == token);
                    var functionResult = CollapseAggregateFunction(transaction, query, aggregationValues, auxiliaryFields, functions, subFunction);

                    string mathVariable = $"v{variableNumber++}";
                    expressionVariables.Add(mathVariable, double.Parse(functionResult));
                    expressionString = expressionString.Replace(token, mathVariable);
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    //This is a string placeholder, get the literal value and complain about it.

                    throw new KbEngineException($"Could not perform mathematical operation on [{query.Batch.GetLiteralValue(token)}]");
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a numeric placeholder, get the literal value and append it.

                    string mathVariable = $"v{variableNumber++}";
                    expressionString = expressionString.Replace(token, mathVariable);
                    expressionVariables.Add(mathVariable, double.Parse(query.Batch.GetLiteralValue(token)));
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbEngineException($"Function parameter string sub-type is not implemented: [{token}].");
                }
            }

            var expressionHash = Library.Helpers.GetSHA1Hash(expressionString);

            //Perhaps we can pass in a cache object?
            var expression = new NCalc.Expression(expressionString);

            foreach (var expressionVariable in expressionVariables)
            {
                expression.Parameters[expressionVariable.Key] = expressionVariable.Value;
            }

            return expression.Evaluate()?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Takes a string expression string and concatenates all of the values, including those from all
        ///     recursive function calls. Concatenation which is really the only operation we support for strings.
        /// </summary>
        static string CollapseAggregateFunctionStringParameter(Transaction transaction, PreparedQuery query,
            KbInsensitiveDictionary<List<string>> aggregationValues, KbInsensitiveDictionary<string?> auxiliaryFields,
            List<IQueryFieldExpressionFunction> functions, string expressionString)
        {
            var tokenizer = new TokenizerSlim(expressionString, ['+', '(', ')']);
            string token;

            var sb = new StringBuilder();

            while (!tokenizer.Exausted())
            {
                token = tokenizer.EatGetNext();

                if (token.StartsWith("$f_") && token.EndsWith('$'))
                {
                    //Resolve the token to a field identifier.
                    if (query.SelectFields.DocumentIdentifiers.TryGetValue(token, out var fieldIdentifier))
                    {
                        //Resolve the field identifier to a value.
                        if (auxiliaryFields.TryGetValue(fieldIdentifier.Value, out var textValue))
                        {
                            sb.Append(textValue);
                        }
                        else
                        {
                            throw new KbEngineException($"Function parameter auxiliary field is not defined: [{token}].");
                        }
                    }
                    else
                    {
                        throw new KbEngineException($"Function parameter field is not defined: [{token}].");
                    }
                }
                else if (token.StartsWith("$x_") && token.EndsWith('$'))
                {
                    //Search the dependency functions for the one with the expression key, this is the one we need to recursively resolve to fill in this token.
                    var subFunction = functions.Single(o => o.ExpressionKey == token);
                    var functionResult = CollapseAggregateFunction(transaction, query, aggregationValues, auxiliaryFields, functions, subFunction);
                    sb.Append(functionResult);
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    //This is a string placeholder, get the literal value and append it.
                    sb.Append(query.Batch.GetLiteralValue(token));
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a numeric placeholder, get the literal value and append it.
                    sb.Append(query.Batch.GetLiteralValue(token));
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbEngineException($"Function parameter string sub-type is not implemented: [{token}].");
                }
                else
                {
                    sb.Append(query.Batch.GetLiteralValue(token));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Takes a function and recursively collapses all of the parameters, then recursively
        ///     executes all dependency functions to collapse the function to a single value.
        /// </summary>
        static string CollapseAggregateFunction(Transaction transaction, PreparedQuery query,
            KbInsensitiveDictionary<List<string>> aggregationValues, KbInsensitiveDictionary<string?> auxiliaryFields,
            List<IQueryFieldExpressionFunction> functions, IQueryFieldExpressionFunction function)
        {
            var collapsedParameters = new List<string>();

            //The sole parameter for aggregate functions is pre-computed by the query execution engine, just get the values.
            if (aggregationValues.TryGetValue(function.ExpressionKey, out var aggregationValueList) != true)
            {
                throw new KbEngineException($"The aggregate function [{function.FunctionName}] resolved expression key was not found: [{function.ExpressionKey}].");
            }

            return AggregateFunctionImplementation.ExecuteFunction(function.FunctionName, aggregationValueList);
        }
    }
}
