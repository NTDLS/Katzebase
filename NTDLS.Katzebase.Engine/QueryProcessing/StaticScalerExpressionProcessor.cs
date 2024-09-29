using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Parsers.Query;
using NTDLS.Katzebase.Parsers.Query.Exposed;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Query.Functions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using System.Text;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Parsers.Query.Fields.Expressions.ExpressionConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing
{
    internal static class StaticScalerExpressionProcessor
    {
        /// <summary>
        /// Resolves all of the query expressions (string concatenation, math and all recursive
        ///     function calls) on a row level and fills in the values in the resultingRows.
        /// </summary>
        public static void CollapseScalerRowExpressions(this SchemaIntersectionRowCollection resultingRows, Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection)
        {
            //Resolve all expressions and fill in the row fields.
            foreach (var expressionField in fieldCollection.ExpressionFields.Where(o => o.CollapseType == CollapseType.Scaler))
            {
                foreach (var row in resultingRows)
                {
                    var collapsedResult = CollapseScalerExpression(transaction, query, fieldCollection, row.AuxiliaryFields, expressionField);
                    row.InsertValue(expressionField.FieldAlias, expressionField.Ordinal, collapsedResult);
                }
            }
        }

        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static string? CollapseScalerQueryField(this IQueryField queryField, Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<string?> auxiliaryFields)
        {
            if (queryField is QueryFieldExpressionNumeric expressionNumeric)
            {
                return CollapseScalerFunctionNumericParameter(transaction, query, fieldCollection, auxiliaryFields, expressionNumeric.FunctionDependencies, expressionNumeric.Value);
            }
            else if (queryField is QueryFieldExpressionString expressionString)
            {
                return CollapseScalerFunctionStringParameter(transaction, query, fieldCollection, auxiliaryFields, expressionString.FunctionDependencies, expressionString.Value);
            }
            else if (queryField is QueryFieldDocumentIdentifier documentIdentifier)
            {
                if (auxiliaryFields.TryGetValue(documentIdentifier.Value, out var exactAuxiliaryValue))
                {
                    return exactAuxiliaryValue ?? string.Empty; //TODO: Should auxiliaryFields really allow NULL values?
                }
                if (auxiliaryFields.TryGetValue(documentIdentifier.FieldName, out var auxiliaryValue))
                {
                    return auxiliaryValue ?? string.Empty; //TODO: Should auxiliaryFields really allow NULL values?
                }
                throw new KbEngineException($"Auxiliary fields not found: [{documentIdentifier.Value}].");
            }
            else if (queryField is QueryFieldConstantNumeric constantNumeric)
            {
                return query.Batch.GetLiteralValue(constantNumeric.Value);
            }
            else if (queryField is QueryFieldConstantString constantString)
            {
                return query.Batch.GetLiteralValue(constantString.Value);
            }
            else if (queryField is QueryFieldCollapsedValue collapsedValue)
            {
                return collapsedValue.Value;
            }
            else
            {
                throw new KbEngineException($"Field expression type is not implemented: [{queryField.GetType().Name}].");
            }
        }

        /// <summary>
        /// Collapses a QueryField expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        public static string? CollapseScalerExpressionFunctionParameter(this IExpressionFunctionParameter parameter, Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<string?> auxiliaryFields, List<IQueryFieldExpressionFunction> functionDependencies)
        {
            if (parameter is ExpressionFunctionParameterString parameterString)
            {
                return CollapseScalerFunctionStringParameter(transaction, query, fieldCollection, auxiliaryFields, functionDependencies, parameterString.Expression);
            }
            else if (parameter is ExpressionFunctionParameterNumeric parameterNumeric)
            {
                return CollapseScalerFunctionNumericParameter(transaction, query, fieldCollection, auxiliaryFields, functionDependencies, parameterNumeric.Expression);
            }
            else if (parameter is ExpressionFunctionParameterFunction expressionFunctionParameterFunction)
            {
                return CollapseScalerFunctionNumericParameter(transaction, query, fieldCollection, auxiliaryFields, functionDependencies, expressionFunctionParameterFunction.Expression);
            }
            else
            {
                throw new KbEngineException($"Function parameter type is not implemented [{parameter.GetType().Name}].");
            }
        }

        /// <summary>
        /// Collapses a string or numeric expression into a single value. This includes doing string concatenation, math and all recursive function calls.
        /// </summary>
        private static string? CollapseScalerExpression(Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<string?> auxiliaryFields, ExposedExpression expression)
        {
            if (expression.FieldExpression is QueryFieldExpressionNumeric expressionNumeric)
            {
                return CollapseScalerFunctionNumericParameter(transaction, query, fieldCollection, auxiliaryFields, expressionNumeric.FunctionDependencies, expressionNumeric.Value);
            }
            else if (expression.FieldExpression is QueryFieldExpressionString expressionString)
            {
                return CollapseScalerFunctionStringParameter(transaction, query, fieldCollection, auxiliaryFields, expressionString.FunctionDependencies, expressionString.Value);
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
        private static string? CollapseScalerFunctionNumericParameter(Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<string?> auxiliaryFields,
            List<IQueryFieldExpressionFunction> functions, string expressionString)
        {
            //Build a cachable numeric expression, interpolate the values and execute the expression.

            var tokenizer = new TokenizerSlim(expressionString, TokenizerExtensions.MathematicalCharacters);

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
                        if (auxiliaryFields.TryGetValue(fieldIdentifier.Value, out var textValue))
                        {
                            textValue.EnsureNotNull();
                            string mathVariable = $"v{variableNumber++}";
                            expressionString = expressionString.Replace(token, mathVariable);
                            expressionVariables.Add(mathVariable, query.Batch.GetLiteralValue(textValue));
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
                    var functionResult = CollapseScalerFunction(transaction, query, fieldCollection, auxiliaryFields, functions, subFunction);

                    string mathVariable = $"v{variableNumber++}";
                    expressionVariables.Add(mathVariable, functionResult);
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
                    expressionVariables.Add(mathVariable, query.Batch.GetLiteralValue(token));
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbEngineException($"Function parameter string sub-type is not implemented: [{token}].");
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

            //Perhaps we can pass in a cache object?
            var expression = new NCalc.Expression(expressionString);

            foreach (var expressionVariable in expressionVariables)
            {
                expression.Parameters[expressionVariable.Key] = expressionVariable.Value == null ? null : double.Parse(expressionVariable.Value);
            }

            return expression.Evaluate()?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Takes a string expression string and concatenates all of the values, including those from all
        ///     recursive function calls. Concatenation which is really the only operation we support for strings.
        /// </summary>
        private static string CollapseScalerFunctionStringParameter(Transaction transaction,
            PreparedQuery query, QueryFieldCollection fieldCollection, KbInsensitiveDictionary<string?> auxiliaryFields,
            List<IQueryFieldExpressionFunction> functions, string expressionString)
        {
            var tokenizer = new TokenizerSlim(expressionString, ['+', '(', ')']);
            string token;

            var stringResult = new StringBuilder();

            //We keep track of potential numeric operations with the mathBuffer and when whenever we encounter a string token
            //  we will compute the previously buffered mathematical expression using ComputeAndClearMathBuffer() and append
            //  the result before processing the string token.
            //
            //  This way we can string compute expressions like "11 ^ 3 + 'ten' + 10 * 10" as "8ten100"
            var mathBuffer = new StringBuilder();

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
                        if (auxiliaryFields.TryGetValue(fieldIdentifier.Value, out var textValue))
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
                    var functionResult = CollapseScalerFunction(transaction, query, fieldCollection, auxiliaryFields, functions, subFunction);

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

                    stringResult.Append(query.Batch.GetLiteralValue(token));
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a numeric placeholder, get the literal value and append it.
                    mathBuffer.Append(query.Batch.GetLiteralValue(token));
                }
                else if (token.StartsWith('$') && token.EndsWith('$'))
                {
                    throw new KbEngineException($"Function parameter string sub-type is not implemented: [{token}].");
                }
                else
                {
                    var value = query.Batch.GetLiteralValue(token);

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
                var mathResult = CollapseScalerFunctionNumericParameter(transaction, query, fieldCollection, auxiliaryFields, functions, mathBuffer.ToString());
                stringResult.Append(mathResult);
                mathBuffer.Clear();
            }

            string? ComputeAndClearMathBuffer(StringBuilder mathBuffer)
            {
                if (mathBuffer.Length > 0)
                {
                    char lastMathCharacter = mathBuffer[mathBuffer.Length - 1];
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

                            throw new KbEngineException($"Cannot perform [{mathBuffer[mathBuffer.Length - 1]}] math on string.");
                        }
                    }

                    //We found a string operator and we have math work in the buffer, collapse the math and append the result.
                    var mathResult = CollapseScalerFunctionNumericParameter(transaction, query, fieldCollection, auxiliaryFields, functions, mathBuffer.ToString());
                    mathBuffer.Clear();
                    return mathResult;
                }

                return string.Empty;
            }

            return stringResult.ToString();
        }


        /// <summary>
        /// Takes a function and recursively collapses all of the parameters, then recursively
        ///     executes all dependency functions to collapse the function to a single value.
        /// </summary>
        static string CollapseScalerFunction(Transaction transaction, PreparedQuery query, QueryFieldCollection fieldCollection,
            KbInsensitiveDictionary<string?> auxiliaryFields, List<IQueryFieldExpressionFunction> functions, IQueryFieldExpressionFunction function)
        {
            var collapsedParameters = new List<string?>();

            foreach (var parameter in function.Parameters)
            {
                collapsedParameters.Add(parameter.CollapseScalerExpressionFunctionParameter(transaction, query, fieldCollection, auxiliaryFields, functions));
            }

            if (AggregateFunctionCollection.TryGetFunction(function.FunctionName, out _))
            {
                throw new KbEngineException($"Cannot perform scaler operation on aggregate result of: [{function.FunctionName}].");
            }

            //Execute function with the parameters from above ↑
            var methodResult = ScalerFunctionImplementation.ExecuteFunction(transaction, function.FunctionName, collapsedParameters, auxiliaryFields);

            //TODO: think through the nullability here.
            return methodResult ?? string.Empty;
        }
    }
}
