using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Functions.Aggregate;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Query.Functions;
using NTDLS.Katzebase.Parsers.Tokens;
using System.Text;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public class StaticParserField
    {
        /// <summary>
        /// Parses a field expression containing fields, functions. string and math operations.
        /// </summary>
        public static IQueryField Parse(Tokenizer parentTokenizer, string givenFieldText, QueryFieldCollection queryFields)
        {
            Tokenizer tokenizer = new(givenFieldText);

            string token = tokenizer.EatGetNext();

            if (tokenizer.IsExhausted()) //This is a single value (document field, number or string), the simple case.
            {
                if (token.IsQueryFieldIdentifier())
                {
                    if (ScalarFunctionCollection.TryGetFunction(token, out var _))
                    {
                        //This is a function call, but it is the only token - that's not a valid function call.
                        throw new KbParserException(parentTokenizer.GetCurrentLineNumber(), $"Scalar function must be called with parentheses: [{token}]");
                    }
                    if (AggregateFunctionCollection.TryGetFunction(token, out var _))
                    {
                        //This is a function call, but it is the only token - that's not a valid function call.
                        throw new KbParserException(parentTokenizer.GetCurrentLineNumber(), $"Aggregate function must be called with parentheses: [{token}]");
                    }

                    if (parentTokenizer.Variables.Collection.TryGetValue(token, out var variableOrConstant))
                    {
                        if (variableOrConstant.IsConstant)
                        {
                            if (variableOrConstant.DataType == KbBasicDataType.Numeric)
                            {
                                return new QueryFieldConstantNumeric(parentTokenizer.GetCurrentLineNumber(), variableOrConstant.Value);
                            }

                            //String and "undefined" data type.
                            return new QueryFieldConstantString(parentTokenizer.GetCurrentLineNumber(), variableOrConstant.Value);
                        }
                    }

                    var queryFieldDocumentIdentifier = new QueryFieldDocumentIdentifier(parentTokenizer.GetCurrentLineNumber(), token);
                    var fieldKey = queryFields.GetNextDocumentFieldKey();
                    queryFields.DocumentIdentifiers.Add(fieldKey, queryFieldDocumentIdentifier);

                    return queryFieldDocumentIdentifier;
                }
                else if (IsNumericExpression(parentTokenizer, token))
                {
                    return new QueryFieldConstantNumeric(parentTokenizer.GetCurrentLineNumber(), token);
                }
                else
                {
                    return new QueryFieldConstantString(parentTokenizer.GetCurrentLineNumber(), token);
                }
            }

            //Fields that require expression evaluation.
            var validateNumberOfParameters = givenFieldText.ScopeSensitiveSplit(',');
            if (validateNumberOfParameters.Count > 1)
            {
                //We are testing to make sure that there are no commas that fall outside of function scopes.
                //This is because each call to ParseField should collapse to a single value.
                //E.g. "10 + Length() * 10" is allowed, but "10 + Length() * 10, Length()" is not allowed.
                throw new KbParserException(parentTokenizer.GetCurrentLineNumber(), $"Field contains multiple values: [{givenFieldText}]");
            }

            //This field is going to require evaluation, so figure out if its a number or a string.
            if (IsNumericExpression(parentTokenizer, givenFieldText))
            {
                IQueryFieldExpression expression = new QueryFieldExpressionNumeric(parentTokenizer.GetCurrentLineNumber(), givenFieldText);
                expression.Value = ParseEvaluationRecursive(parentTokenizer, ref expression, givenFieldText, ref queryFields);
                return expression;
            }
            else
            {
                IQueryFieldExpression expression = new QueryFieldExpressionString(parentTokenizer.GetCurrentLineNumber(), givenFieldText);
                expression.Value = ParseEvaluationRecursive(parentTokenizer, ref expression, givenFieldText, ref queryFields);
                return expression;
            }
        }

        private static string ParseEvaluationRecursive(Tokenizer parentTokenizer, ref IQueryFieldExpression rootQueryFieldExpression,
            string givenExpressionText, ref QueryFieldCollection queryFields)
        {
            Tokenizer tokenizer = new(givenExpressionText);

            StringBuilder buffer = new();

            while (!tokenizer.IsExhausted())
            {
                string token = tokenizer.GetNext();

                if (token == "(")
                {
                    var scopeText = tokenizer.EatGetMatchingScope();
                    var parenthesesResult = ParseEvaluationRecursive(parentTokenizer, ref rootQueryFieldExpression, scopeText, ref queryFields);

                    buffer.Append($"({parenthesesResult})");
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$')) //A string placeholder.
                {
                    tokenizer.EatNext();
                    buffer.Append(token);
                }
                else if (token.StartsWith("$v_") && token.EndsWith('$')) //A variable placeholder.
                {
                    tokenizer.EatNext();
                    buffer.Append(token);
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$')) //A numeric placeholder.
                {
                    tokenizer.EatNext();
                    buffer.Append(token);
                }
                else if (ScalarFunctionCollection.TryGetFunction(token, out var scalarFunction))
                {
                    tokenizer.EatNext();

                    if (!tokenizer.TryIsNextNonIdentifier(['(']))
                    {
                        throw new KbParserException(parentTokenizer.GetCurrentLineNumber(), $"Scalar function must be called with parentheses: [{token}]");
                    }
                    //The expression key is used to match the function calls to the token in the parent expression.
                    var expressionKey = queryFields.GetNextExpressionKey();
                    var basicDataType = scalarFunction.ReturnType == KbScalarFunctionParameterType.Numeric ? KbBasicDataType.Numeric : KbBasicDataType.String;
                    var queryFieldExpressionFunction = new QueryFieldExpressionFunctionScalar(scalarFunction.Name, expressionKey, basicDataType);

                    ParseFunctionCallRecursive(parentTokenizer, ref rootQueryFieldExpression, queryFieldExpressionFunction, ref queryFields, tokenizer);

                    buffer.Append(expressionKey);
                }
                else if (AggregateFunctionCollection.TryGetFunction(token, out var aggregateFunction))
                {
                    tokenizer.EatNext();
                    if (!tokenizer.TryIsNextNonIdentifier(['(']))
                    {
                        throw new KbParserException(parentTokenizer.GetCurrentLineNumber(), $"Aggregate function must be called with parentheses: [{token}]");
                    }

                    //The expression key is used to match the function calls to the token in the parent expression.
                    var expressionKey = queryFields.GetNextExpressionKey();
                    var queryFieldExpressionFunction = new QueryFieldExpressionFunctionAggregate(aggregateFunction.Name, expressionKey, KbBasicDataType.Numeric);

                    ParseFunctionCallRecursive(parentTokenizer, ref rootQueryFieldExpression, queryFieldExpressionFunction, ref queryFields, tokenizer);

                    //A common way to call count(0) is count(*). Check for this special case here and replace '*' with '0'.
                    //There is no difference between count(0), count(*) or count(<any constant value>).
                    if (aggregateFunction.Name.Is("Count")
                        && queryFieldExpressionFunction.Parameters.Count == 1
                        && queryFieldExpressionFunction.Parameters[0].Expression.Is("*"))
                    {
                        queryFieldExpressionFunction.Parameters[0].Expression = "0";
                    }

                    buffer.Append(expressionKey);
                }
                else if (token.IsQueryFieldIdentifier())
                {
                    tokenizer.EatNext();
                    if (tokenizer.TryIsNextNonIdentifier(['(']))
                    {
                        //tokenizer.EatNext();
                        //The character after this identifier is an open parenthesis, so this
                        //  looks like a function call but the function is undefined.
                        throw new KbParserException(parentTokenizer.GetCurrentLineNumber(), $"Function is undefined: [{token}].");
                    }

                    var fieldKey = queryFields.GetNextDocumentFieldKey();
                    queryFields.DocumentIdentifiers.Add(fieldKey, new QueryFieldDocumentIdentifier(parentTokenizer.GetCurrentLineNumber(), token));
                    buffer.Append(fieldKey);
                }
                else
                {
                    tokenizer.EatNext();
                    buffer.Append(token);
                }

                if (!tokenizer.IsExhausted())
                {
                    //Verify that the next character (if any) is a "connector".
                    if (tokenizer.NextCharacter != null && !tokenizer.TryIsNextCharacter(o => o.IsTokenConnectorCharacter()))
                    {
                        throw new KbParserException(parentTokenizer.GetCurrentLineNumber(), $"Connection token is missing, found: [{parentTokenizer.Variables.Resolve(token)}].");
                    }
                    else
                    {
                        buffer.Append(tokenizer.EatNextCharacter());
                    }
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Parses a function call and its parameters, add them to the passed queryFieldExpressionFunction.
        /// </summary>
        private static void ParseFunctionCallRecursive(Tokenizer parentTokenizer, ref IQueryFieldExpression rootQueryFieldExpression,
            IQueryFieldExpressionFunction queryFieldExpressionFunction, ref QueryFieldCollection queryFields, Tokenizer tokenizer)
        {
            //This contains the text between the open and close parenthesis of a function call, but not the parenthesis themselves or the function name.
            string functionCallParametersSegmentText = tokenizer.EatGetMatchingScope('(', ')');

            var functionCallParametersText = functionCallParametersSegmentText.ScopeSensitiveSplit(',');
            foreach (var functionCallParameterText in functionCallParametersText)
            {
                //Recursively process the function parameters.
                var resultingExpressionString = ParseEvaluationRecursive(parentTokenizer, ref rootQueryFieldExpression, functionCallParameterText, ref queryFields);

                IExpressionFunctionParameter? parameter = null;

                //TODO: ensure that this is a single token.
                if (resultingExpressionString.StartsWith("$x_") && resultingExpressionString.EndsWith('$') && IsSingleToken(resultingExpressionString))
                {
                    //This is a function call result placeholder.
                    parameter = new ExpressionFunctionParameterFunction(resultingExpressionString);
                }
                else if (IsNumericExpression(parentTokenizer, resultingExpressionString, rootQueryFieldExpression.FunctionDependencies))
                {
                    //This expression contains only numeric placeholders.
                    parameter = new ExpressionFunctionParameterNumeric(resultingExpressionString);
                }
                else
                {
                    //This expression contains non-numeric placeholders.
                    parameter = new ExpressionFunctionParameterString(resultingExpressionString);
                }

                queryFieldExpressionFunction.Parameters.Add(parameter);
            }

            rootQueryFieldExpression.FunctionDependencies.Add(queryFieldExpressionFunction);
        }

        private static bool IsSingleToken(string token)
        {
            if (token.StartsWith('$') == token.EndsWith('$') && token.Length >= 5) //Example: $n_0$, $x_0$
            {
                if (char.IsLetter(token[1]) && token[2] == '_')
                {
                    return token[3..^1].All(char.IsDigit); //Validate the number in the middle of the markers.
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if all variables, placeholders and functions return numeric values.
        /// </summary>
        public static bool IsNumericExpression(Tokenizer parentTokenizer, string expressionText, List<IQueryFieldExpressionFunction>? functionDependencies = null)
        {
            Tokenizer tokenizer = new(expressionText, [' ', '+']);

            while (!tokenizer.IsExhausted())
            {
                if (tokenizer.TryIsNextCharacter(c => c.IsMathematicalOperator()))
                {
                    tokenizer.EatNextCharacter();
                    continue;
                }

                string token = tokenizer.EatGetNext();
                if (string.IsNullOrEmpty(token))
                {
                    break;
                }

                if (token.StartsWith("$x_") && token.EndsWith('$'))
                {
                    //This is a function result placeholder.
                    if (functionDependencies == null)
                    {
                        throw new KbParserException(parentTokenizer.GetCurrentLineNumber(), $"Function reference found without root expression: [{token}].");
                    }

                    //Find the function call so we can check the function return type.
                    var referencedFunction = functionDependencies.Single(f => f.ExpressionKey == token);

                    if (referencedFunction.ReturnType != KbBasicDataType.Numeric)
                    {
                        //This function returns something other then numeric, so we are evaluating strings.
                        return false;
                    }
                    continue;
                }
                else if (token.StartsWith("$v_") && token.EndsWith('$'))
                {
                    if (tokenizer.Variables.Collection.TryGetValue(token, out var variable))
                    {
                        if (variable.DataType != KbBasicDataType.Numeric)
                        {
                            return false;
                        }
                        continue;
                    }
                    return false;
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    //This is a string, so this is not a numeric operation.
                    return false;
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a number placeholder, so we still have a valid numeric operation.
                    continue;
                }
                else if (ScalarFunctionCollection.TryGetFunction(token, out var scalarFunction))
                {
                    if (scalarFunction.ReturnType == KbScalarFunctionParameterType.Numeric)
                    {
                        //This function returns a number, so we still have a valid numeric operation.

                        //Skip the function call.
                        string functionBody = tokenizer.EatGetMatchingScope('(', ')');
                        continue;
                    }
                    else
                    {
                        //This function returns a non-number, so this is not a numeric operation.
                        return false;
                    }
                }
                else if (AggregateFunctionCollection.TryGetFunction(token, out var aggregateFunction))
                {
                    if (aggregateFunction.ReturnType == KbAggregateFunctionParameterType.Numeric)
                    {
                        //This function returns a number, so we still have a valid numeric operation.

                        //Skip the function call.
                        string functionBody = tokenizer.EatGetMatchingScope('(', ')');
                        continue;
                    }
                    else
                    {
                        //This function returns a non-number, so this is not a numeric operation.
                        return false;
                    }
                }
                else
                {
                    //This is likely a document field, we'll assume its a number
                    //return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if all variables and placeholders are constant expressions (e.g. does not contain document fields or functions).
        /// </summary>
        public static bool IsConstantExpression(string? expressionText)
        {
            if (expressionText == null)
            {
                return true;
            }

            Tokenizer tokenizer = new(expressionText, [' ', '+']);

            while (!tokenizer.IsExhausted())
            {
                if (tokenizer.TryIsNextCharacter(c => c.IsMathematicalOperator()))
                {
                    tokenizer.EatNextCharacter();
                    continue;
                }

                string token = tokenizer.EatGetNext();
                if (string.IsNullOrEmpty(token))
                {
                    break;
                }

                if (token.StartsWith("$x_") && token.EndsWith('$'))
                {
                    //This is a function result placeholder, and functions are not constant.
                    return false;
                }
                else if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    //This is a string constant, we're all good.
                    continue;
                }
                else if (token.StartsWith("$v_") && token.EndsWith('$'))
                {
                    //This is a variable, we're all good.
                    continue;
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a numeric constant, we're all good.
                    continue;
                }
                else if (ScalarFunctionCollection.TryGetFunction(token, out var scalarFunction))
                {
                    //Functions are not constant.
                    return false;
                }
                else if (AggregateFunctionCollection.TryGetFunction(token, out var aggregateFunction))
                {
                    //Functions are not constant.
                    return false;
                }
                else
                {
                    //This is likely a document field, we'll assume its a number
                    return false;
                }
            }

            return true;
        }
    }
}
