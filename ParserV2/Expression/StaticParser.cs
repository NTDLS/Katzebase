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
        public static Expressions ParseSelectFields(Tokenizer queryTokenizer)
        {
            var result = new Expressions();

            //Get the position which represents the end of the select list.
            int stopAt = queryTokenizer.InertGetNextIndexOf([" from ", " into "]);

            //Get the text for all of the select expressions.
            var fieldsText = queryTokenizer.SubString(stopAt);

            //Split the select expressions on the comma, respecting any commas in function scopes.
            var fields = fieldsText.ScopeSensitiveSplit();

            foreach (var field in fields)
            {
                ParseExpression(field, queryTokenizer);
            }

            return result;
        }

        private static IExpression ParseExpression(string text, Tokenizer queryTokenizer)
        {
            //var result = new Expressions();

            Tokenizer tokenizer = new(text, queryTokenizer.TokenDelimiters); //These delimiters have not been through through at all!

            if (tokenizer.InertIsNextCharacter(char.IsLetter) && tokenizer.InertIsNextNonIdentifier(['(']))
            {
                var functionName = tokenizer.GetNext();

                string functionScopeText = tokenizer.GetMatchingBraces('(', ')');

                var parameterStrings = functionScopeText.ScopeSensitiveSplit();

                foreach (var paramText in parameterStrings)
                {
                    if (IsNumericOperation(paramText))
                    {
                        if (DoesNumericRequireEvaluation(paramText))
                        {
                            var result = new ExpressionNumericEvaluation();
                            return result;
                        }
                        else
                        {
                            var result = new ExpressionConstant("0", ExpressionConstantType.Numeric);
                            return result;
                        }
                    }
                    else
                    {
                        if (DoesStringRequireEvaluation(paramText))
                        {
                            var result = new ExpressionStringEvaluation();
                            return result;
                        }
                        else
                        {
                            var result = new ExpressionConstant("?????", ExpressionConstantType.String);
                            return result;
                        }
                    }
                }
            }

            throw new KbParserException($"Unable to parse expression: [{text}]");
        }

        /// <summary>
        /// Determines if the numeric expression is a simple standalone number or if it requires some type of evaluation.
        /// The evaluation requirement can be as simple as "10 + 10" or a nested set of function calls.
        /// </summary>
        /// <returns></returns>
        private static bool DoesNumericRequireEvaluation(string text)
        {
            return true;
        }

        /// <summary>
        /// Determines if the string expression is a simple standalone string or if it requires some type of evaluation.
        /// The evaluation requirement can be as simple as ["Hello" + "World"] or a nested set of function calls.
        /// </summary>
        /// <returns></returns>
        private static bool DoesStringRequireEvaluation(string text)
        {
            return true;
        }

        /// <summary>
        /// Returns true if all variables, placeholders and functions return numeric values.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="KbParserException"></exception>
        private static bool IsNumericOperation(string text)
        {
            Tokenizer tokenizer = new(text, [',', '=']); //These delimiters have not been through through at all!

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
                    throw new KbParserException($"Invalid query. Found [{token}], expected: scaler function.");
                }
            }

            return true;
        }


    }
}
