using NTDLS.Katzebase.Client.Exceptions;
using ParserV2.StandIn;
using static ParserV2.Expression.ExpressionConstant;
using static ParserV2.StandIn.Types;

namespace ParserV2.Expression
{
    internal static class StaticParser
    {
        /// <summary>
        /// Parses the field expressions for a "select" or "select into" query.
        /// </summary>
        public static ExpressionCollection ParseSelectFields(Tokenizer queryTokenizer)
        {
            var result = new ExpressionCollection();

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

                result.Collection.Add(new NamedExpression(fieldExpressionAlias, expression));
            }

            return result;
        }

        private static IExpression ParseExpression(string givenExpressionText, Tokenizer queryTokenizer)
        {
            #region This is a single value expression, the simple case.

            Tokenizer tokenizer = new(givenExpressionText, queryTokenizer.TokenDelimiters); //These delimiters have not been thought through at all!

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
                    return new ExpressionIdentifier(token);

                }
                else if (IsNumericExpression(token))
                {
                    if (DoesNumericRequireEvaluation(token))
                    {
                        throw new KbParserException($"Simple expression should not require evaluation: [{token}]");
                    }
                    else
                    {
                        return new ExpressionConstant(token, ExpressionConstantType.Numeric);
                    }
                }
                else
                {
                    if (DoesStringRequireEvaluation(token))
                    {
                        throw new KbParserException($"Simple expression should not require evaluation: [{token}]");
                    }
                    else
                    {
                        return new ExpressionConstant(token, ExpressionConstantType.String);
                    }
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

            if (IsNumericExpression(givenExpressionText))
            {
                if (DoesNumericRequireEvaluation(givenExpressionText))
                {
                    return RecursiveEvaluation(givenExpressionText);
                }
                else
                {
                    throw new KbParserException($"Complex expression should require evaluation: [{givenExpressionText}]");
                }
            }
            else
            {
                if (DoesStringRequireEvaluation(givenExpressionText))
                {
                    return RecursiveEvaluation(givenExpressionText);
                }
                else
                {
                    throw new KbParserException($"Complex expression should require evaluation: [{givenExpressionText}]");
                }
            }

            #endregion
        }

        private static ExpressionEvaluation RecursiveEvaluation(string givenExpressionText)
        {
            Tokenizer tokenizer = new(givenExpressionText, [',', '=']); //These delimiters have not been thought through at all!

            /*
            while (!tokenizer.IsEnd())
            {
                token = tokenizer.GetNext();
            }
            tokenizer.Rewind();
            */

            #region Unused debug code.

            /*
            //Parse a function call: TODO: this doesn't work if the call is something like "10 + Length()"
            if (tokenizer.InertIsNextCharacter(char.IsLetter) && tokenizer.InertIsNextNonIdentifier(['(']))
            {
                var functionName = tokenizer.GetNext();

                string functionParameterExpressions = tokenizer.GetMatchingBraces('(', ')');
                var expressionParameterTexts = functionParameterExpressions.ScopeSensitiveSplit();

                foreach (var expressionParameterText in expressionParameterTexts)
                {
                    IExpression singleExpression;

                    if (IsNumericExpression(givenExpressionText))
                    {
                        if (DoesNumericRequireEvaluation(expressionParameterText))
                        {
                            singleExpression = new ExpressionNumericEvaluation();
                        }
                        else
                        {
                            singleExpression = new ExpressionConstant(expressionParameterText, ExpressionConstantType.Numeric);
                        }
                    }
                    else
                    {
                        if (DoesStringRequireEvaluation(expressionParameterText))
                        {
                            singleExpression = new ExpressionStringEvaluation();
                        }
                        else
                        {
                            singleExpression = new ExpressionConstant(expressionParameterText, ExpressionConstantType.String);
                        }
                    }


                }
            }
            return new ExpressionConstant("dummy", ExpressionConstantType.String);
            //throw new KbParserException($"Unable to parse expression: [{givenExpressionText}]");
            */

            #endregion

            return new ExpressionEvaluation();
        }


        /// <summary>
        /// Determines if the numeric expression is a simple standalone number or if it requires some type of evaluation.
        /// The evaluation requirement can be as simple as "10 + 10" or a nested set of function calls.
        /// </summary>
        /// <returns></returns>
        private static bool DoesNumericRequireEvaluation(string expressionText)
        {
            Tokenizer tokenizer = new(expressionText, [',', '=']); //These delimiters have not been thought through at all!
            var token = tokenizer.GetNext();

            if (!(token.StartsWith("$n_") && token.EndsWith('$')))
            {
                //If the token is not a number placeholder than we will need to evaluate the expression to determine its value.
                return true;
            }

            //If more text remains after getting a single token, then we will need to evaluate the expression to determine its value.
            return !tokenizer.IsEnd();
        }

        /// <summary>
        /// Determines if the string expression is a simple standalone string or if it requires some type of evaluation.
        /// The evaluation requirement can be as simple as ["Hello" + "World"] or a nested set of function calls.
        /// </summary>
        /// <returns></returns>
        private static bool DoesStringRequireEvaluation(string expressionText)
        {
            Tokenizer tokenizer = new(expressionText, [',', '=']); //These delimiters have not been thought through at all!
            var token = tokenizer.GetNext();

            if (!(token.StartsWith("$s_") && token.EndsWith('$')))
            {
                //If the token is not a string placeholder than we will need to evaluate the expression to determine its value.
                return true;
            }

            //If more text remains after getting a single token, then we will need to evaluate the expression to determine its value.
            return !tokenizer.IsEnd();
        }

        /// <summary>
        /// Returns true if all variables, placeholders and functions return numeric values.
        /// </summary>
        /// <param name="expressionText"></param>
        /// <returns></returns>
        /// <exception cref="KbParserException"></exception>
        private static bool IsNumericExpression(string expressionText)
        {
            Tokenizer tokenizer = new(expressionText, [',', '=']); //These delimiters have not been thought through at all!

            while (true)
            {
                string token = tokenizer.GetNext(['(', '+']);
                if (string.IsNullOrEmpty(token))
                {
                    break;
                }

                if (tokenizer.InertIsNextCharacter('+'))
                {
                    tokenizer.SkipNextCharacter();
                }

                if (token.StartsWith("$s_") && token.EndsWith('$'))
                {
                    //This is a string, so this is not a numeric operation.
                    return false;
                }

                if (token.StartsWith("$n_") && token.EndsWith('$'))
                {
                    //This is a number placeholder, so we still have a valid numeric operation.
                    continue;
                }

                if (ScalerFunctionCollection.TryGetFunction(token, out var function))
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


    }
}
