using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers;
using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Functions;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using System.Text;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Expressions
{
    /// <summary>
    /// This code it highly redundant to StaticScalarExpressionProcessor, but is highly paired down. These functions DO NOT execute
    /// the expression or its functions but rather return a string that explains the expression. This is mainly used to determine if
    /// an expression in the select list is referenced in the GROUP BY.
    /// <seealso cref="StaticScalarExpressionProcessor.CollapseScalarQueryField()" />
    /// </summary>
    public static class StaticScalarExpressionSimplification
    {
        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static string? SimplifyScalarQueryField(this IQueryField queryField, PreparedQuery query, QueryFieldCollection fieldCollection)
        {
            if (queryField is QueryFieldExpressionNumeric expressionNumeric)
            {
                return SimplifyScalarNumericExpression(query, fieldCollection,
                    expressionNumeric.FunctionDependencies, expressionNumeric.Value)?.AssertUnresolvedExpression();
            }
            else if (queryField is QueryFieldExpressionString expressionString)
            {
                return SimplifyScalarStringExpression(query, fieldCollection,
                    expressionString.FunctionDependencies, expressionString.Value)?.AssertUnresolvedExpression();
            }
            else if (queryField is QueryFieldDocumentIdentifier documentIdentifier)
            {
                //documentIdentifier.Value contains the schema qualified field name.
                return documentIdentifier.Value.EnsureNotNull();
            }
            else if (queryField is QueryFieldConstantNumeric constantNumeric)
            {
                return query.Batch.Variables.Resolve(constantNumeric.Value)?.AssertUnresolvedExpression();
            }
            else if (queryField is QueryFieldConstantString constantString)
            {
                return query.Batch.Variables.Resolve(constantString.Value)?.AssertUnresolvedExpression();
            }
            else if (queryField is QueryFieldCollapsedValue collapsedValue)
            {
                return collapsedValue.Value?.AssertUnresolvedExpression();
            }
            else
            {
                throw new KbNotImplementedException($"Field expression type is not implemented: [{queryField.GetType().Name}].");
            }
        }

        internal static string? SimplifyScalarNumericExpression(PreparedQuery query, QueryFieldCollection fieldCollection,
            List<IQueryFieldExpressionFunction> functions, string? givenExpressionString)
        {
            if (givenExpressionString == null)
            {
                return null;
            }
            var expressionString = new StringBuilder(givenExpressionString);

            var tokenizer = new TokenizerSlim(expressionString.ToString().EnsureNotNull(), TokenizerExtensions.MathematicalCharacters);

            int variableNumber = 0;

            var expressionVariables = new Dictionary<string, string?>();

            while (!tokenizer.IsExhausted())
            {
                var token = tokenizer.EatGetNext();

                if (token.StartsWith("$f_") && token.EndsWith('$'))
                {
                    //Resolve the token to a field identifier.
                    if (fieldCollection.DocumentIdentifiers.TryGetValue(token, out var fieldIdentifier))
                    {
                        string mathVariable = $"v{variableNumber++}";
                        expressionString.Replace(token, fieldIdentifier.Value);
                        expressionVariables.Add(mathVariable, query.Batch.Variables.Resolve(fieldIdentifier.Value));
                    }
                    else
                    {
                        throw new KbEngineException($"Function parameter field is not defined: [{token}].");
                    }
                }
                else if (token.StartsWith("$x_") && token.EndsWith('$'))
                {
                    //Search the dependency functions for the one with the expression key, this is the
                    //  one we need to recursively resolve to fill in this token.
                    var subFunction = functions.Single(o => o.ExpressionKey == token);

                    if (AggregateFunctionCollection.TryGetFunction(subFunction.FunctionName, out var aggregateFunction))
                    {
                        var functionResult = subFunction.SimplifyAggregateFunction();
                        string mathVariable = $"v{variableNumber++}";
                        expressionVariables.Add(mathVariable, functionResult);
                        expressionString.Replace(token, mathVariable);
                    }
                    else if (ScalarFunctionCollection.TryGetFunction(subFunction.FunctionName, out var scalarFunction))
                    {
                        var functionResult = subFunction.SimplifyScalarFunction(query, fieldCollection, functions);
                        string mathVariable = $"v{variableNumber++}";
                        expressionVariables.Add(mathVariable, functionResult);
                        expressionString.Replace(token, mathVariable);
                    }
                    else
                    {
                        throw new KbEngineException($"Unknown function type: [{subFunction.FunctionName}].");
                    }
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    //This is a string placeholder, get the literal value and complain about it.
                    throw new KbProcessingException($"Could not perform mathematical operation on [{query.Batch.Variables.Resolve(token)}]");
                }
                else if (token.StartsWith("$v_") && token.EndsWith('$'))
                {
                    var resolved = query.Batch.Variables.Resolve(token, out var dataType);
                    if (dataType == KbBasicDataType.Numeric)
                    {
                        //This is a numeric variable, get the value and append it.
                        string mathVariable = $"v{variableNumber++}";
                        expressionString.Replace(token, mathVariable);
                        expressionVariables.Add(mathVariable, resolved);
                    }
                    else
                    {
                        throw new KbProcessingException($"Could not perform mathematical operation on [{query.Batch.Variables.Resolve(token)}]");
                    }
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a numeric placeholder, get the literal value and append it.
                    string mathVariable = $"v{variableNumber++}";
                    expressionString.Replace(token, mathVariable);
                    expressionVariables.Add(mathVariable, query.Batch.Variables.Resolve(token));
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbNotImplementedException($"Function parameter string sub-type is not implemented: [{token}].");
                }
            }

            if (expressionVariables.Count == 1)
            {
                //If this is the only token we have then we aren't even going to do math.
                //This is because this is more efficient and also because this might be a
                //string value from a document field that we assumed was numeric because the
                //expression contains no "string operations" such as literal text.

                //We do "best effort" math.
                return expressionVariables.First().Value;
            }

            if (expressionVariables.Any(o => o.Value == null))
            {
                return null;
            }

            return expressionString.ToString();
        }

        internal static string? SimplifyScalarStringExpression(PreparedQuery query, QueryFieldCollection fieldCollection,
            List<IQueryFieldExpressionFunction> functions,
            string? givenExpressionString)
        {
            if (givenExpressionString == null)
            {
                return null;
            }

            var tokenizer = new TokenizerSlim(givenExpressionString, ['+', '(', ')']);
            string token;

            var stringResult = new NullPropagationString();

            //We keep track of potential numeric operations with the mathBuffer and when whenever we encounter a string token
            //  we will compute the previously buffered mathematical expression using ComputeAndClearMathBuffer() and append
            //  the result before processing the string token.
            //
            //  This way we can string compute expressions like "11 ^ 3 + 'ten' + 10 * 10" as "8ten100"
            var mathBuffer = new NullPropagationString();

            while (!tokenizer.IsExhausted())
            {
                int previousCaret = tokenizer.Caret;

                token = tokenizer.EatGetNext(TokenizerExtensions.MathematicalCharacters, out var stoppedAtCharacter);

                if (stoppedAtCharacter == '(')
                {
                    tokenizer.SetCaret(previousCaret);
                    var scopeText = tokenizer.EatGetMatchingScope();
                    mathBuffer.Append($"({scopeText})");
                    continue;
                }
                else if (token.StartsWith("$f_") && token.EndsWith('$'))
                {
                    //Resolve the token to a field identifier.
                    if (fieldCollection.DocumentIdentifiers.TryGetValue(token, out var fieldIdentifier))
                    {
                        return fieldIdentifier.Value.EnsureNotNull();
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
                    var functionResult = subFunction.SimplifyScalarFunction(query, fieldCollection, functions);

                    if (subFunction.ReturnType == KbBasicDataType.Numeric)
                    {
                        mathBuffer.Append(functionResult);
                    }
                    else
                    {
                        if (mathBuffer.Length > 0)
                        {
                            stringResult.Append(ComputeAndClearMathBuffer(mathBuffer));
                        }

                        stringResult.Append(functionResult);
                    }
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    if (mathBuffer.Length > 0)
                    {
                        stringResult.Append(ComputeAndClearMathBuffer(mathBuffer));
                    }

                    stringResult.Append(query.Batch.Variables.Resolve(token));
                }
                else if (token.StartsWith("$v_") && token.EndsWith('$'))
                {
                    var resolved = query.Batch.Variables.Resolve(token, out var dataType);
                    if (dataType == KbBasicDataType.Numeric)
                    {
                        //This is a numeric variable, get the value and append it.
                        mathBuffer.Append(resolved);
                    }
                    else
                    {
                        //Variable was not numeric, terminate the match buffer(if any) and build the string.
                        if (mathBuffer.Length > 0)
                        {
                            stringResult.Append(ComputeAndClearMathBuffer(mathBuffer));
                        }

                        stringResult.Append(query.Batch.Variables.Resolve(token));
                    }
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a numeric placeholder, get the literal value and append it.
                    mathBuffer.Append(query.Batch.Variables.Resolve(token));
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbNotImplementedException($"Function parameter string sub-type is not implemented: [{token}].");
                }
                else
                {
                    var value = query.Batch.Variables.Resolve(token);

                    if (double.TryParse(value, out _))
                    {
                        mathBuffer.Append(value);
                    }
                    else
                    {
                        if (mathBuffer.Length > 0)
                        {
                            stringResult.Append(ComputeAndClearMathBuffer(mathBuffer));
                        }
                        stringResult.Append(value);
                    }
                }

                if (mathBuffer.Length > 0 && stoppedAtCharacter != null)
                {
                    mathBuffer.Append(stoppedAtCharacter);
                }
            }

            if (mathBuffer.Length > 0)
            {
                //We found a string operator and we have math work in the buffer, collapse the math and append the result.
                var mathResult = SimplifyScalarNumericExpression(query, fieldCollection, functions, mathBuffer.ToString());
                stringResult.Append(mathResult);
                mathBuffer.Clear();
            }

            string? ComputeAndClearMathBuffer(NullPropagationString mathBuffer)
            {
                if (mathBuffer.Length > 0)
                {
                    char lastMathCharacter = mathBuffer[^1];
                    if (lastMathCharacter.IsMathematicalOperator())
                    {
                        if (lastMathCharacter == '+')
                        {
                            //The parent function is designed to parse strings, the only connecting mathematical operator we support
                            //  from a string to a mathematical expression is '+' (concatenate). So validate that if we have a
                            //  mathematical operator that is '+'.
                            //
                            mathBuffer.Length--; //Remove the trailing '+' operator from the mathematical expression.
                        }
                        else if (lastMathCharacter != ')')
                        {
                            //Because we parse expressions within parentheses separately, we avoid the whole mess of appending a
                            //  trailing '+' operator. So if the last character is a closing parentheses, just process the math,
                            //  otherwise throw an exception because we only allow ')' and '+' as the trailing expression character.

                            throw new KbProcessingException($"Cannot perform [{mathBuffer[^1]}] math on string.");
                        }
                    }

                    //We found a string operator and we have math work in the buffer, collapse the math and append the result.
                    var mathResult = SimplifyScalarNumericExpression(query, fieldCollection, functions, mathBuffer.ToString());
                    mathBuffer.Clear();
                    return mathResult;
                }

                return string.Empty;
            }

            return stringResult.RealizedValue();
        }

        /// <summary>
        /// Takes a function and recursively collapses all of the parameters, then recursively
        ///     executes all dependency functions to collapse the function to a single value.
        /// </summary>
        public static string? SimplifyScalarFunction(this IQueryFieldExpressionFunction function, PreparedQuery query,
            QueryFieldCollection fieldCollection, List<IQueryFieldExpressionFunction> functions)
        {
            var collapsedParameters = new List<string?>();

            foreach (var parameter in function.Parameters)
            {
                collapsedParameters.Add(parameter.SimplifyScalarExpression(query, fieldCollection, functions));
            }

            return $"{function.FunctionName}({string.Join(',', collapsedParameters)})";
        }

        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static string? SimplifyScalarExpression(this IExpressionFunctionParameter parameter, PreparedQuery query,
            QueryFieldCollection fieldCollection, List<IQueryFieldExpressionFunction> functionDependencies)
        {
            if (parameter is ExpressionFunctionParameterString parameterString)
            {
                return SimplifyScalarStringExpression(query, fieldCollection,
                    functionDependencies, parameterString.Expression);
            }
            else if (parameter is ExpressionFunctionParameterNumeric parameterNumeric)
            {
                return SimplifyScalarNumericExpression(query, fieldCollection,
                    functionDependencies, parameterNumeric.Expression);
            }
            else if (parameter is ExpressionFunctionParameterFunction expressionFunctionParameterFunction)
            {
                return SimplifyScalarNumericExpression(query, fieldCollection,
                    functionDependencies, expressionFunctionParameterFunction.Expression);
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
        public static string? SimplifyAggregateFunction(this IQueryFieldExpressionFunction function)
        {
            return $"{function.FunctionName}(...)";
        }
    }
}
