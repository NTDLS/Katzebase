using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Parsers;
using NTDLS.Katzebase.Engine.Parsers.Query.Exposed;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Engine.Parsers.Query.Functions;
using NTDLS.Katzebase.Engine.Query.Searchers.Intersection;
using NTDLS.Katzebase.Engine.Threading.PoolingParameters;
using System.Text;

namespace NTDLS.Katzebase.Engine.Query
{
    internal class StaticExpressionProcessor
    {
        public static void CollapseAllExpressions(Transaction transaction, DocumentLookupOperation operation, SchemaIntersectionRowCollection resultingRows)
        {
            //Resolve all expressions and fill in the row fields.
            foreach (var expressionField in operation.Query.SelectFields.ExpressionFields)
            {
                foreach (var row in resultingRows.Collection)
                {
                    CollapseExpression(transaction, operation, row, expressionField);
                }
            }
        }

        static void CollapseExpression(Transaction transaction, DocumentLookupOperation operation, SchemaIntersectionRow row, ExposedExpression rootExpression)
        {
            if (rootExpression.FieldExpression is QueryFieldExpressionNumeric expressionNumeric)
            {
                var expressionResult = CollapseScalerFunctionNumericParameter(transaction, operation, row, expressionNumeric.FunctionDependencies, expressionNumeric.Value);
                row.InsertValue(rootExpression.FieldAlias, rootExpression.Ordinal, expressionResult);
            }
            else if (rootExpression.FieldExpression is QueryFieldExpressionString expressionString)
            {
                var expressionResult = CollapseScalerFunctionStringParameter(transaction, operation, row, expressionString.FunctionDependencies, expressionString.Value);
                row.InsertValue(rootExpression.FieldAlias, rootExpression.Ordinal, expressionResult);
            }
            else
            {
                throw new KbEngineException($"Field expression type is not implemented: [{rootExpression.FieldExpression.GetType().Name}].");
            }
        }

        static string CollapseScalerFunction(Transaction transaction, DocumentLookupOperation operation,
            SchemaIntersectionRow row, List<IQueryFieldExpressionFunction> functions, IQueryFieldExpressionFunction function)
        {
            var collapsedParameters = new List<string>();

            foreach (var parameter in function.Parameters)
            {
                var collapsedParameter = CollapseScalerFunctionParameter(transaction, operation, row, functions, function, parameter);

                collapsedParameters.Add(collapsedParameter);
            }

            //Execute function with the parameters from above ↑
            var methodResult = ScalerFunctionImplementation.ExecuteFunction(transaction, function.FunctionName, collapsedParameters, row.AuxiliaryFields);

            //TODO: think through the nullability here.
            return methodResult ?? string.Empty;
        }

        static string CollapseScalerFunctionParameter(Transaction transaction,
            DocumentLookupOperation operation, SchemaIntersectionRow row,
            List<IQueryFieldExpressionFunction> functions, IQueryFieldExpressionFunction function,
            IExpressionFunctionParameter parameter)
        {
            if (parameter is ExpressionFunctionParameterString parameterString)
            {
                return CollapseScalerFunctionStringParameter(transaction, operation, row, functions, parameterString.Expression);
            }
            else if (parameter is ExpressionFunctionParameterNumeric parameterNumeric)
            {
                return CollapseScalerFunctionNumericParameter(transaction, operation, row, functions, parameterNumeric.Expression);
            }
            else
            {
                throw new KbEngineException($"Function parameter type is not implemented [{parameter.GetType().Name}].");
            }
        }

        /// <summary>
        /// Takes a string expression and appends all of the values, which is really the only operation we support for strings.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="operation"></param>
        /// <param name="row"></param>
        /// <param name="function"></param>
        /// <param name="parameter"></param>
        /// <exception cref="KbEngineException"></exception>
        static string CollapseScalerFunctionStringParameter(Transaction transaction,
            DocumentLookupOperation operation, SchemaIntersectionRow row,
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
                    if (operation.Query.SelectFields.DocumentIdentifiers.TryGetValue(token, out var fieldIdentifier))
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
                    var functionResult = CollapseScalerFunction(transaction, operation, row, functions, subFunction);
                    sb.Append(functionResult);
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    sb.Append(operation.Query.Batch.GetLiteralValue(token));
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    sb.Append(operation.Query.Batch.GetLiteralValue(token));
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbEngineException($"Function parameter string sub-type is not implemented: [{token}].");
                }
                else
                {
                    sb.Append(operation.Query.Batch.GetLiteralValue(token));
                }
            }

            return sb.ToString();
        }

        static string CollapseScalerFunctionNumericParameter(Transaction transaction,
            DocumentLookupOperation operation, SchemaIntersectionRow row,
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
                    var functionResult = CollapseScalerFunction(transaction, operation, row, functions, subFunction);

                    string mathVariable = $"v{variableNumber++}";
                    expressionVariables.Add(mathVariable, double.Parse(functionResult));
                    evaluationExpression = evaluationExpression.Replace(token, mathVariable);
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    string mathVariable = $"v{variableNumber++}";
                    evaluationExpression = evaluationExpression.Replace(token, mathVariable);
                    expressionVariables.Add(mathVariable, double.Parse(operation.Query.Batch.GetLiteralValue(token)));
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    string mathVariable = $"v{variableNumber++}";
                    evaluationExpression = evaluationExpression.Replace(token, mathVariable);
                    expressionVariables.Add(mathVariable, double.Parse(operation.Query.Batch.GetLiteralValue(token)));
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbEngineException($"Function parameter string sub-type is not implemented: [{token}].");
                }
            }

            var expressionHash = Library.Helpers.GetSHA1Hash(evaluationExpression);

            NCalc.Expression? expression = null;
            operation.ExpressionCache.UpgradableRead(r =>
            {
                if (r.TryGetValue(expressionHash, out expression) == false)
                {
                    expression = new NCalc.Expression(evaluationExpression);
                    operation.ExpressionCache.Write(w => w.Add(expressionHash, expression));
                }
            });

            if (expression == null) throw new KbEngineException($"Expression cannot be null.");

            foreach (var expressionVariable in expressionVariables)
            {
                expression.Parameters[expressionVariable.Key] = expressionVariable.Value;
            }

            return expression.Evaluate()?.ToString() ?? string.Empty;
        }
    }
}
