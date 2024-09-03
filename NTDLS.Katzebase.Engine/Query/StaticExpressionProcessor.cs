using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Parsers;
using NTDLS.Katzebase.Engine.Parsers.Query.Exposed;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Engine.Parsers.Query.Functions;
using NTDLS.Katzebase.Engine.Query.Searchers.Intersection;
using System.Text;

namespace NTDLS.Katzebase.Engine.Query
{
    internal class StaticExpressionProcessor
    {
        /// <summary>
        /// Resolves all of the query expressions on a row level and fills in the values in the resultingRows.
        /// </summary>
        public static void CollapseRowExpressions(Transaction transaction, PreparedQuery query, SchemaIntersectionRowCollection resultingRows)
        {
            //Resolve all expressions and fill in the row fields.
            foreach (var expressionField in query.SelectFields.ExpressionFields)
            {
                foreach (var row in resultingRows.Collection)
                {
                    var collapsedResult = CollapseExpression(transaction, query, row, expressionField);
                    row.InsertValue(expressionField.FieldAlias, expressionField.Ordinal, collapsedResult);
                }
            }
        }

        /// <summary>
        /// Collapses a string or numeric expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static string CollapseExpression(Transaction transaction, PreparedQuery query, SchemaIntersectionRow row, ExposedExpression expression)
        {
            if (expression.FieldExpression is QueryFieldExpressionNumeric expressionNumeric)
            {
                return CollapseScalerFunctionNumericParameter(transaction, query, row, expressionNumeric.FunctionDependencies, expressionNumeric.Value);
            }
            else if (expression.FieldExpression is QueryFieldExpressionString expressionString)
            {
                return CollapseScalerFunctionStringParameter(transaction, query, row, expressionString.FunctionDependencies, expressionString.Value);
            }
            else
            {
                throw new KbEngineException($"Field expression type is not implemented: [{expression.FieldExpression.GetType().Name}].");
            }
        }

        /// <summary>
        /// Takes a string expression string and performs math on all of the values, including those from all
        ///     recursive function calls.
        /// </summary>
        static string CollapseScalerFunctionNumericParameter(Transaction transaction,
            PreparedQuery query, SchemaIntersectionRow row,
            List<IQueryFieldExpressionFunction> functions, string expressionString)
        {
            //Build a cachable numeric expression, interpolate the values and execute the expression.

            var tokenizer = new TokenizerSlim(expressionString, ['~', '!', '%', '^', '&', '*', '(', ')', '-', '/', '+']);
            string evaluationExpression = new string(expressionString.ToCharArray());
            int variableNumber = 0;

            var expressionVariables = new Dictionary<string, double>();

            while (tokenizer.IsEnd() == false)
            {
                var token = tokenizer.GetNext();

                if (token.StartsWith("$x_") && token.EndsWith('$'))
                {
                    var subFunction = functions.Single(o => o.ExpressionKey == token);
                    var functionResult = CollapseScalerFunction(transaction, query, row, functions, subFunction);

                    string mathVariable = $"v{variableNumber++}";
                    expressionVariables.Add(mathVariable, double.Parse(functionResult));
                    evaluationExpression = evaluationExpression.Replace(token, mathVariable);
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    string mathVariable = $"v{variableNumber++}";
                    evaluationExpression = evaluationExpression.Replace(token, mathVariable);
                    expressionVariables.Add(mathVariable, double.Parse(query.Batch.GetLiteralValue(token)));
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    string mathVariable = $"v{variableNumber++}";
                    evaluationExpression = evaluationExpression.Replace(token, mathVariable);
                    expressionVariables.Add(mathVariable, double.Parse(query.Batch.GetLiteralValue(token)));
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbEngineException($"Function parameter string sub-type is not implemented: [{token}].");
                }
            }

            var expressionHash = Library.Helpers.GetSHA1Hash(evaluationExpression);

            NCalc.Expression? expression = null;
            query.ExpressionCache.UpgradableRead(r =>
            {
                if (r.TryGetValue(expressionHash, out expression) == false)
                {
                    expression = new NCalc.Expression(evaluationExpression);
                    query.ExpressionCache.Write(w => w.Add(expressionHash, expression));
                }
            });

            if (expression == null) throw new KbEngineException($"Expression cannot be null.");

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
        static string CollapseScalerFunctionStringParameter(Transaction transaction,
            PreparedQuery query, SchemaIntersectionRow row,
            List<IQueryFieldExpressionFunction> functions, string expressionString)
        {
            var tokenizer = new TokenizerSlim(expressionString, ['+', '(', ')']);
            string token;

            var sb = new StringBuilder();

            while (tokenizer.IsEnd() == false)
            {
                token = tokenizer.GetNext();

                if (token.StartsWith("$f_") && token.EndsWith('$'))
                {
                    //Resolve the token to a field identifier.
                    if (query.SelectFields.DocumentIdentifiers.TryGetValue(token, out var fieldIdentifier))
                    {
                        //Resolve the field identifier to a value.
                        if (row.AuxiliaryFields.TryGetValue(fieldIdentifier.Value, out var textValue))
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
                    var subFunction = functions.Single(o => o.ExpressionKey == token);
                    var functionResult = CollapseScalerFunction(transaction, query, row, functions, subFunction);
                    sb.Append(functionResult);
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    sb.Append(query.Batch.GetLiteralValue(token));
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
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
        static string CollapseScalerFunction(Transaction transaction, PreparedQuery query,
            SchemaIntersectionRow row, List<IQueryFieldExpressionFunction> functions, IQueryFieldExpressionFunction function)
        {
            var collapsedParameters = new List<string>();

            foreach (var parameter in function.Parameters)
            {
                if (parameter is ExpressionFunctionParameterString parameterString)
                {
                    var collapsedParameter = CollapseScalerFunctionStringParameter(transaction, query, row, functions, parameterString.Expression);
                }
                else if (parameter is ExpressionFunctionParameterNumeric parameterNumeric)
                {
                    var collapsedParameter = CollapseScalerFunctionNumericParameter(transaction, query, row, functions, parameterNumeric.Expression);
                    collapsedParameters.Add(collapsedParameter);
                }
                else
                {
                    throw new KbEngineException($"Function parameter type is not implemented [{parameter.GetType().Name}].");
                }
            }

            //Execute function with the parameters from above ↑
            var methodResult = ScalerFunctionImplementation.ExecuteFunction(transaction, function.FunctionName, collapsedParameters, row.AuxiliaryFields);

            //TODO: think through the nullability here.
            return methodResult ?? string.Empty;
        }
    }
}
