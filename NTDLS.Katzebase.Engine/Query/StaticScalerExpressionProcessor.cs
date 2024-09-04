﻿using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Parsers.Query.Exposed;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Engine.Parsers.Query.Functions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using NTDLS.Katzebase.Engine.Query.Searchers.Intersection;
using System.Text;
using static NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions.ExpressionConstants;

namespace NTDLS.Katzebase.Engine.Query
{
    internal class StaticScalerExpressionProcessor
    {
        /// <summary>
        /// Resolves all of the query expressions (string concatenation, math and all recursive
        ///     function calls) on a row level and fills in the values in the resultingRows.
        /// </summary>
        public static void CollapseScalerRowExpressions(Transaction transaction, PreparedQuery query, ref SchemaIntersectionRowCollection resultingRows)
        {
            //Resolve all expressions and fill in the row fields.
            foreach (var expressionField in query.SelectFields.ExpressionFields.Where(o => o.CollapseType == CollapseType.Scaler))
            {
                foreach (var row in resultingRows.Collection)
                {
                    var collapsedResult = CollapseScalerExpression(transaction, query, row, expressionField);
                    row.InsertValue(expressionField.FieldAlias, expressionField.Ordinal, collapsedResult);
                }
            }
        }

        /// <summary>
        /// Collapses a string or numeric expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static string CollapseScalerExpression(Transaction transaction, PreparedQuery query, SchemaIntersectionRow row, ExposedExpression expression)
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

            int variableNumber = 0;

            var expressionVariables = new Dictionary<string, double>();

            while (!tokenizer.Exausted())
            {
                var token = tokenizer.EatGetNext();

                if (token.StartsWith("$x_") && token.EndsWith('$'))
                {
                    //Search the dependency functions for the one with the expression key, this is the one we need to recursively resolve to fill in this token.

                    var subFunction = functions.Single(o => o.ExpressionKey == token);

                    if (ScalerFunctionCollection.TryGetFunction(subFunction.FunctionName, out var scalerFunction))
                    {
                    }
                    if (AggregateFunctionCollection.TryGetFunction(subFunction.FunctionName, out var aggregateFunction))
                    {
                    }

                    var functionResult = CollapseScalerFunction(transaction, query, row, functions, subFunction);

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
        static string CollapseScalerFunctionStringParameter(Transaction transaction,
            PreparedQuery query, SchemaIntersectionRow row,
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
                    //Search the dependency functions for the one with the expression key, this is the one we need to recursively resolve to fill in this token.
                    var subFunction = functions.Single(o => o.ExpressionKey == token);
                    var functionResult = CollapseScalerFunction(transaction, query, row, functions, subFunction);
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
        static string CollapseScalerFunction(Transaction transaction, PreparedQuery query,
            SchemaIntersectionRow row, List<IQueryFieldExpressionFunction> functions, IQueryFieldExpressionFunction function)
        {
            var collapsedParameters = new List<string>();

            foreach (var parameter in function.Parameters)
            {
                if (parameter is ExpressionFunctionParameterString parameterString)
                {
                    var collapsedParameter = CollapseScalerFunctionStringParameter(transaction, query, row, functions, parameterString.Expression);
                    collapsedParameters.Add(collapsedParameter);
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
