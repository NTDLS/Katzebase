using NTDLS.Katzebase.Client.Exceptions;
using ParserV2.Parsers.Query.Fields;
using ParserV2.Parsers.Query.Fields.Expressions;
using ParserV2.Parsers.Query.Functions;
using ParserV2.StandIn;
using System.Text;
using static ParserV2.StandIn.Types;

namespace ParserV2.Parsers.Query
{
    internal static class StaticQueryParser
    {
        /// <summary>
        /// Parses the field expressions for a "select" or "select into" query.
        /// </summary>
        public static QueryFieldCollection ParseSelectFields(Tokenizer queryTokenizer)
        {
            var result = new QueryFieldCollection();

            //Get the position which represents the end of the select list.
            int stopAt = queryTokenizer.InertGetNextIndexOf([" from ", " into "]);

            //Get the text for all of the select expressions.
            var fieldsText = queryTokenizer.SubString(stopAt);

            //Split the select expressions on the comma, respecting any commas in function scopes.
            var fieldExpressionTexts = fieldsText.ScopeSensitiveSplit();

            foreach (var fieldExpressionText in fieldExpressionTexts)
            {
                string fieldExpressionAlias = string.Empty;

                //Parse the field expression alias.
                int aliasIndex = fieldExpressionText.IndexOf(" as ", StringComparison.InvariantCultureIgnoreCase);
                if (aliasIndex > 0)
                {
                    fieldExpressionAlias = fieldExpressionText.Substring(aliasIndex + 4).Trim();
                }

                var aliasRemovedFieldExpressionText = (aliasIndex > 0 ? fieldExpressionText.Substring(0, aliasIndex) : fieldExpressionText).Trim();

                var expression = ParseExpression(aliasRemovedFieldExpressionText, queryTokenizer);

                result.Collection.Add(new QueryField(fieldExpressionAlias, expression));
            }

            return result;
        }

        private static IQueryField ParseExpression(string givenExpressionText, Tokenizer queryTokenizer)
        {
            #region This is a single value expression (document field, number or string), the simple case.

            Tokenizer tokenizer = new(givenExpressionText);

            string token = tokenizer.GetNext();

            if (tokenizer.IsEnd())
            {
                if (token.IsIdentifier())
                {
                    if (ScalerFunctionCollection.TryGetFunction(token, out var _))
                    {
                        //This is a function call, but it is the only token - that's not a valid function call.
                        throw new KbParserException($"Simple expression function expects parentheses: [{token}]");
                    }

                    //This is not a function (those require evaluation) so its a single identifier, likely a document field name.
                    return new QueryFieldDocumentIdentifier(token);

                }
                else if (IsNumericExpression(token))
                {
                    return new QueryFieldConstantNumeric(token);
                }
                else
                {
                    return new QueryFieldConstantString(token);
                }
            }

            #endregion

            #region Expressions that require evaluation.

            var collapsibleExpressionEvaluation = givenExpressionText.ScopeSensitiveSplit();
            if (collapsibleExpressionEvaluation.Count > 1)
            {
                //We are testing to make sure that there are no commas that fall outside of function scopes.
                //This is because each call to ParseExpression should collapse to a single value.
                //E.g. "10 + Length() * 10" is allowed, but "10 + Length() * 10, Length()" is not allowed.
                throw new KbParserException($"Single expression should contain multiple values: [{givenExpressionText}]");
            }

            //This expression is going to require evaluation, so figure out if its a number or a string.

            if (IsNumericExpression(givenExpressionText))
            {
                IQueryFieldExpression expression = new QueryFieldExpressionNumeric(givenExpressionText);
                expression.Expression = ParseEvaluationRecursive(ref expression, givenExpressionText, out _);
                return expression;
            }
            else
            {
                IQueryFieldExpression expression = new QueryFieldExpressionString();
                expression.Expression = ParseEvaluationRecursive(ref expression, givenExpressionText, out _);
                return expression;
            }

            #endregion
        }

        private static string ParseEvaluationRecursive(ref IQueryFieldExpression rootExpressionEvaluation,
            string givenExpressionText, out List<FunctionReference> outReferencedFunctions)
        {
            outReferencedFunctions = new();

            Tokenizer tokenizer = new(givenExpressionText);

            StringBuilder buffer = new();


            while (!tokenizer.IsEnd())
            {
                int positionBeforeToken = tokenizer.CaretPosition;

                string token = tokenizer.GetNext();

                if (token.StartsWith("$s_") && token.EndsWith('$')) //A string placeholder.
                {
                    buffer.Append(token);
                }
                else if (token.StartsWith("$n_") && token.EndsWith('$')) //A numeric placeholder.
                {
                    buffer.Append(token);
                }
                else if (ScalerFunctionCollection.TryGetFunction(token, out var function))
                {
                    string functionParameterExpressions = tokenizer.GetMatchingBraces('(', ')');
                    string wholeFunctionExpression = tokenizer.InertSubString(positionBeforeToken, tokenizer.CaretPosition - positionBeforeToken);

                    //The expression key is used to match the function calls to the token in the parent expression.
                    var expressionKey = rootExpressionEvaluation.GetKeyExpressionKey();

                    givenExpressionText = givenExpressionText.Replace(wholeFunctionExpression, expressionKey);

                    var functionCallEvaluation = new QueryFieldExpressionFunction(function.Name, expressionKey, function.ReturnType);

                    var referencedFunction = new FunctionReference(expressionKey, function.Name, function.ReturnType);
                    rootExpressionEvaluation.ReferencedFunctions.Add(referencedFunction);
                    outReferencedFunctions.Add(referencedFunction);

                    var expressionParameterTexts = functionParameterExpressions.ScopeSensitiveSplit();
                    foreach (var functionParameter in expressionParameterTexts)
                    {
                        List<FunctionReference> referencedFunctions = new();

                        //Recursively process the function parameters.
                        var resultingExpressionString = ParseEvaluationRecursive(ref rootExpressionEvaluation, functionParameter, out referencedFunctions);

                        IExpressionFunctionParameter? parameter = null;

                        functionCallEvaluation.ReferencedFunctions.AddRange(referencedFunctions);

                        if (resultingExpressionString.StartsWith("$p_") && resultingExpressionString.EndsWith('$'))
                        {
                            //This is a function call result placeholder.
                            parameter = new ExpressionFunctionParameterFunction(resultingExpressionString);
                            parameter.ReferencedFunctions.AddRange(referencedFunctions);
                        }
                        else if (IsNumericExpression(resultingExpressionString, rootExpressionEvaluation))
                        {
                            //This expression contains only numeric placeholders.
                            parameter = new ExpressionFunctionParameterNumeric(resultingExpressionString);
                            parameter.ReferencedFunctions.AddRange(referencedFunctions);
                        }
                        else
                        {
                            //This expression contains non-numeric placeholders.
                            parameter = new ExpressionFunctionParameterString(resultingExpressionString);
                            parameter.ReferencedFunctions.AddRange(referencedFunctions);
                        }

                        functionCallEvaluation.Parameters.Add(parameter);
                    }

                    rootExpressionEvaluation.FunctionCalls.Add(functionCallEvaluation);
                }
                else if (token.IsIdentifier())
                {
                    if (tokenizer.InertIsNextNonIdentifier(['(']))
                    {
                        //The character after this identifier is an open parenthesis, so this
                        //  looks like a function call but the function is undefined.
                        throw new KbParserException($"Function [{token}] is undefined.");
                    }
                }
                else
                {
                    buffer.Append(token);
                }

            }

            return givenExpressionText;
        }

        /// <summary>
        /// /// Returns true if all variables, placeholders and functions return numeric values.
        /// </summary>
        /// <param name="expressionText">The text to be evaluated for numeric or string operations.</param>
        /// <param name="rootExpressionEvaluation">Passed if available to determine the return type of any function references.</param>
        /// <returns></returns>
        private static bool IsNumericExpression(string expressionText, IQueryFieldExpression? rootExpressionEvaluation = null)
        {
            Tokenizer tokenizer = new(expressionText);

            while (!tokenizer.IsEnd())
            {
                if (tokenizer.InertIsNextCharacter(c => c.IsMathematicalOperator()))
                {
                    tokenizer.SkipNextCharacter();
                    continue;
                }

                string token = tokenizer.GetNext();
                if (string.IsNullOrEmpty(token))
                {
                    break;
                }

                if (token.StartsWith("$x_") && token.EndsWith('$'))
                {
                    //This is a function result placeholder.

                    if (rootExpressionEvaluation == null)
                    {
                        throw new KbParserException($"Function reference found without root expression: [{token}].");
                    }
                    
                    //var referencedFunction = rootExpressionEvaluation.ReferencedFunctions.Single(f => f.Key == token);
                    var referencedFunction = rootExpressionEvaluation.FunctionCalls.Single(f => f.ExpressionKey == token);

                    if (referencedFunction.ReturnType != KbScalerFunctionParameterType.Numeric)
                    {
                        //This function returns something other then numeric, so we are evaluating strings.
                        return false;
                    }
                    continue;
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
                else if (ScalerFunctionCollection.TryGetFunction(token, out var function))
                {
                    if (function.ReturnType == KbScalerFunctionParameterType.Numeric)
                    {
                        //This function returns a number, so we still have a valid numeric operation.

                        //Skip the function call.
                        string functionBody = tokenizer.GetMatchingBraces('(', ')');
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
                    //throw new KbParserException($"Invalid query. Found [{token}], expected: scaler function.");

                    //This is likely a document field.
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if the next token in the sequence is a valid token as would be expected as the start of a new query.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNextStartOfQuery(string token, out QueryType type)
        {
            return Enum.TryParse(token.ToLowerInvariant(), true, out type) //Enum parse.
                && Enum.IsDefined(typeof(QueryType), type) //Is enum value über lenient.
                && int.TryParse(token, out _) == false; //Is not number, because enum parsing is "too" flexible.
        }

    }
}
