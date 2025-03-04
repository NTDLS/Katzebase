﻿using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Scalar;
using NTDLS.Katzebase.Engine.QueryProcessing.Expressions;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers;
using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using System.Text;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Engine.Expressions
{
    internal static class StaticScalarExpressionProcessor
    {
        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// <seealso cref="StaticScalarExpressionSimplification.SimplifyScalarQueryField(IQueryField, PreparedQuery, QueryFieldCollection)" />
        /// </summary>
        public static string? CollapseScalarQueryField(this IQueryField queryField, Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<string?> auxiliaryFields)
        {
            if (queryField is QueryFieldExpressionNumeric expressionNumeric)
            {
                return CollapseScalarNumericExpression(transaction, query, fieldCollection, auxiliaryFields,
                    expressionNumeric.FunctionDependencies, expressionNumeric.Value)?.AssertUnresolvedExpression();
            }
            else if (queryField is QueryFieldExpressionString expressionString)
            {
                return CollapseScalarStringExpression(transaction, query, fieldCollection, auxiliaryFields,
                    expressionString.FunctionDependencies, expressionString.Value)?.AssertUnresolvedExpression();
            }
            else if (queryField is QueryFieldDocumentIdentifier documentIdentifier)
            {
                //documentIdentifier.Value contains the schema qualified field name.
                if (auxiliaryFields.TryGetValue(documentIdentifier.Value.EnsureNotNull(), out var exactAuxiliaryValue))
                {
                    return exactAuxiliaryValue?.AssertUnresolvedExpression();
                }

                //documentIdentifier.FieldName contains the field name.
                if (auxiliaryFields.TryGetValue(documentIdentifier.FieldName, out var auxiliaryValue))
                {
                    return auxiliaryValue?.AssertUnresolvedExpression();
                }

                transaction.AddWarning(KbTransactionWarning.FieldNotFound, documentIdentifier.Value);
                return null;
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

        /// <summary>
        /// Takes a function and recursively collapses all of the parameters, then recursively
        ///     executes all dependency functions to collapse the function to a single value.
        /// </summary>
        public static string? CollapseScalarFunction(this IQueryFieldExpressionFunction function, Transaction transaction, PreparedQuery query,
            QueryFieldCollection fieldCollection, KbInsensitiveDictionary<string?> auxiliaryFields, List<IQueryFieldExpressionFunction> functions,
            KbInsensitiveDictionary<GroupAggregateFunctionParameter>? aggregateFunctionParameters = null)
        {
            var collapsedParameters = new List<string?>();

            foreach (var parameter in function.Parameters)
            {
                collapsedParameters.Add(parameter.CollapseScalarExpression(transaction, query, fieldCollection, auxiliaryFields, functions, aggregateFunctionParameters));
            }

            if (AggregateFunctionCollection.TryGetFunction(function.FunctionName, out _))
            {
                if (aggregateFunctionParameters == null)
                {
                    throw new KbProcessingException($"Cannot perform scalar operation on aggregate result of: [{function.FunctionName}].");
                }
            }

            //Execute function with the parameters from above ↑
            var methodResult = ScalarFunctionImplementation.ExecuteFunction(function.FunctionName, collapsedParameters, auxiliaryFields);

            return methodResult;
        }

        /// <summary>
        /// Takes a string expression string and performs math on all of the values, including those from all
        ///     recursive function calls.
        /// </summary>
        internal static string? CollapseScalarNumericExpression(Transaction transaction, PreparedQuery query, QueryFieldCollection fieldCollection,
            KbInsensitiveDictionary<string?> auxiliaryFields, List<IQueryFieldExpressionFunction> functions, string? givenExpressionString,
            KbInsensitiveDictionary<GroupAggregateFunctionParameter>? aggregateFunctionParameters = null)
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
                        //Resolve the field identifier to a value.
                        if (auxiliaryFields.TryGetValue(fieldIdentifier.Value.EnsureNotNull(), out var textValue))
                        {
                            textValue.EnsureNotNull();
                            string mathVariable = $"v{variableNumber++}";
                            expressionString.Replace(token, mathVariable);
                            expressionVariables.Add(mathVariable, query.Batch.Variables.Resolve(textValue));
                        }
                        else
                        {
                            string mathVariable = $"v{variableNumber++}";
                            expressionString.Replace(token, mathVariable);
                            expressionVariables.Add(mathVariable, null);

                            transaction.AddWarning(KbTransactionWarning.FieldNotFound, fieldIdentifier.Value);
                            return null;
                        }
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
                        var functionResult = subFunction.CollapseAggregateFunction(aggregateFunctionParameters.EnsureNotNull());
                        string mathVariable = $"v{variableNumber++}";
                        expressionVariables.Add(mathVariable, functionResult);
                        expressionString.Replace(token, mathVariable);
                    }
                    else if (ScalarFunctionCollection.TryGetFunction(subFunction.FunctionName, out var scalarFunction))
                    {
                        var functionResult = subFunction.CollapseScalarFunction(transaction, query, fieldCollection, auxiliaryFields, functions, aggregateFunctionParameters);
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
                if (expressionVariables.Count > 1)
                {
                    //We only add NullValuePropagation if we have more than one variable,
                    //  because its not a null propagation if its only one value.
                    transaction.AddWarning(KbTransactionWarning.NullValuePropagation);
                }

                return null;
            }

            //Perhaps we can pass in a cache object?
            var expression = new NCalc.Expression(expressionString.ToString());

            foreach (var expressionVariable in expressionVariables)
            {
                expression.Parameters[expressionVariable.Key] = expressionVariable.Value == null ? null : double.Parse(expressionVariable.Value);
            }

            return expression.Evaluate()?.ToString();
        }

        /// <summary>
        /// Takes a string expression string and concatenates all of the values, including those from all
        ///     recursive function calls. Concatenation which is really the only operation we support for strings.
        /// </summary>
        internal static string? CollapseScalarStringExpression(Transaction transaction, PreparedQuery query, QueryFieldCollection fieldCollection,
            KbInsensitiveDictionary<string?> auxiliaryFields, List<IQueryFieldExpressionFunction> functions,
            string? givenExpressionString, KbInsensitiveDictionary<GroupAggregateFunctionParameter>? aggregateFunctionParameters = null)
        {
            if (givenExpressionString == null)
            {
                return null;
            }

            var tokenizer = new TokenizerSlim(givenExpressionString, ['+', '(', ')']);
            string token;

            var stringResult = new NullPropagationString(transaction);

            //We keep track of potential numeric operations with the mathBuffer and when whenever we encounter a string token
            //  we will compute the previously buffered mathematical expression using ComputeAndClearMathBuffer() and append
            //  the result before processing the string token.
            //
            //  This way we can string compute expressions like "11 ^ 3 + 'ten' + 10 * 10" as "8ten100"
            var mathBuffer = new NullPropagationString(transaction);

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
                        //Resolve the field identifier to a value.
                        if (auxiliaryFields.TryGetValue(fieldIdentifier.Value.EnsureNotNull(), out var textValue))
                        {
                            if (double.TryParse(textValue, out _))
                            {
                                mathBuffer.Append(textValue);
                            }
                            else
                            {
                                if (mathBuffer.Length > 0)
                                {
                                    stringResult.Append(ComputeAndClearMathBuffer(mathBuffer));
                                }
                                stringResult.Append(textValue);
                            }
                        }
                        else
                        {
                            transaction.AddWarning(KbTransactionWarning.FieldNotFound, fieldIdentifier.Value);
                            return null;
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
                    var functionResult = subFunction.CollapseScalarFunction(transaction, query, fieldCollection, auxiliaryFields, functions);

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
                var mathResult = CollapseScalarNumericExpression(transaction, query, fieldCollection, auxiliaryFields, functions, mathBuffer.ToString());
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
                    var mathResult = CollapseScalarNumericExpression(transaction, query, fieldCollection, auxiliaryFields, functions, mathBuffer.ToString());
                    mathBuffer.Clear();
                    return mathResult;
                }

                return string.Empty;
            }

            return stringResult.RealizedValue();
        }
    }
}
