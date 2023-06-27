using Katzebase.Engine.Method.ParsedMethodParameter;
using Katzebase.Engine.Query.Tokenizers;
using Katzebase.PublicLibrary.Exceptions;
using System.Text;
using System.Text.RegularExpressions;

namespace Katzebase.Engine.Method
{
    internal class StaticMethodParser
    {
        private static readonly char[] _mathChars = "+-/*!~^()=<>".ToCharArray();

        internal static GenericParsedMethodParameters ParseQueryFields(QueryTokenizer query)
        {
            var preParsed = PreParseQueryFields(query);

            var result = new GenericParsedMethodParameters();

            foreach (var field in preParsed)
            {
                if (field.IsComplex)
                {
                    var methodCall = ParseMethodCall(field.Text);
                    methodCall.Alias = field.Alias;
                    result.Add(methodCall);
                }
                else
                {
                    var newField = new ParsedFieldParameter(field.Text);
                    newField.Alias = field.Alias;
                    result.Add(newField);
                }
            }

            return result;
        }

        private static bool IsNextNonIdentifier(string text, int startPos, char c)
        {
            return IsNextNonIdentifier(text, startPos, new char[] { c });
        }

        private static bool IsNextNonIdentifier(string text, int startPos, char[] c)
        {
            for (int i = startPos; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                }
                else if (char.IsLetterOrDigit(text[i]))
                {
                }
                else if (text[i] == '.')
                {
                }
                else if (c.Contains(text[i]))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        private static ParsedExpression ParseMathExpression(string text)
        {
            var expression = new ParsedExpression();
            string param = string.Empty;

            int paramCount = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '{')
                {
                    while (i < text.Length && text[i] != '}')
                    {
                        param += text[i++];
                    }
                    continue;
                }
                else if (char.IsLetter(c) && IsNextNonIdentifier(text, i, '('))
                {
                    int startPosition = i;
                    int parenScope = 0;

                    for (int endPosition = startPosition; endPosition < text.Length; endPosition++)
                    {
                        c = text[endPosition];

                        if (c == '(')
                        {
                            parenScope++;
                        }
                        else if (c == ')')
                        {
                            parenScope--;

                            if (parenScope == 0)
                            {
                                string subParamText = text.Substring(startPosition, (endPosition - startPosition) + 1);

                                string paramKey = $"{{p{paramCount++}}}";
                                var mathParamParams = (ParsedMethodAndParams)ParseMethodCall(subParamText, paramKey);

                                expression.Parameters.Add(mathParamParams);

                                //Replace first occurance.
                                var regex = new Regex(Regex.Escape(subParamText));
                                text = regex.Replace(text, paramKey, 1);

                                param = string.Empty; //We are starting over.
                                i = -1;
                                break;
                            }
                        }
                    }
                }
                else if (_mathChars.Contains(c) || char.IsDigit(c) || c == '.')
                {
                    param += c;
                }
                else if (c == '$') //Literl string placeholder.
                {
                    param += '$';

                    i++;
                    while (i < text.Length && text[i] != '$')
                    {
                        param += text[i];
                        i++;
                    }
                    param += '$';
                    continue;
                }
                else if (char.IsLetter(c))
                {
                    int startPosition = i;

                    for (int endPosition = startPosition; endPosition < text.Length + 1; endPosition++)
                    {
                        if (endPosition == text.Length || !(char.IsLetterOrDigit(text[endPosition]) || text[endPosition] == '.'))
                        {
                            //We either found the end of the string or found a non identifier character.
                            endPosition--;

                            string subParamText = text.Substring(startPosition, (endPosition - startPosition) + 1);

                            string paramKey = $"{{p{paramCount++}}}";
                            var mathParamParams = new ParsedFieldParameter(subParamText);

                            mathParamParams.ExpressionKey = paramKey;

                            expression.Parameters.Add(mathParamParams);

                            //Replace first occurance.
                            var regex = new Regex(Regex.Escape(subParamText));
                            text = regex.Replace(text, paramKey, 1);

                            param = string.Empty; //We are starting over.
                            i = -1;
                            break;
                        }
                    }
                }
                else
                {
                    throw new KbParserException("Failed to parse mathematical expression.");
                }
            }

            expression.Value = text;

            return expression;
        }

        private static GenericParsedMethodParameter ParseMethodCall(string text, string expressionKey = "")
        {
            char firstChar = text[0];

            if (char.IsNumber(firstChar))
            {
                //Parse math expression.
                return ParseMathExpression(text);
            }
            else if (char.IsLetter(firstChar) && IsNextNonIdentifier(text, 0, "+-/*!~^".ToCharArray()))
            {
                return ParseMathExpression(text);
            }
            else if (char.IsLetter(firstChar) && IsNextNonIdentifier(text, 0, '('))
            {
                //Parse method call with one or more parameters.

                string param = string.Empty;
                int parenScope = 0;
                bool isComplex = false;
                bool parseMath = false;
                int parenIndex = text.IndexOf('(');

                ParsedMethodAndParams results;


                if (expressionKey != string.Empty)
                {
                    results = new ParsedNamedMethodAndParams()
                    {
                        Method = text.Substring(0, parenIndex),
                        ExpressionKey = expressionKey,
                    };
                }
                else
                {
                    results = new ParsedMethodAndParams()
                    {
                        Method = text.Substring(0, parenIndex),
                    };
                }

                bool parenScopeFellToZero = false;

                //Parse parameters:
                for (int i = parenIndex; i < text.Length; i++)
                {
                    char c = text[i];

                    if (parenScopeFellToZero && _mathChars.Contains(c))
                    {
                        //We have finished parsing a full (...) scope for a function and now we are finding math. Reset and just parse math.
                        return ParseMathExpression(text);
                    }

                    if (_mathChars.Contains(c) && !(c == '(' || c == ')'))
                    {
                        //The paramter contains math characters. '(' and ')' are usre for method calls and do not count.
                        parseMath = true;
                    }

                    if (param == string.Empty && char.IsDigit(c))
                    {
                        //The first character of the parameter is a number.
                        parseMath = true;
                    }

                    if (c == '$') //Literl string placeholder.
                    {
                        param += '$';

                        i++;
                        while (i < text.Length && text[i] != '$')
                        {
                            c = text[i];
                            param += text[i];
                            i++;
                        }
                        param += '$';
                        continue;
                    }
                    else if (c == '(')
                    {
                        if (parenScope != 0)
                        {
                            isComplex = true;
                            param += c;
                        }
                        parenScope++;
                    }
                    else if (c == ')')
                    {
                        parenScope--;
                        if (parenScope != 0)
                        {
                            param += c;
                        }

                        if (parenScope == 0)
                        {
                            parenScopeFellToZero = true;
                        }
                    }
                    else if (c == ',')
                    {
                        if (parenScope != 1)
                        {
                            param += c;
                        }
                    }
                    else
                    {
                        param += c;
                    }

                    if ((c == ',' && parenScope == 1) || (i == text.Length - 1))
                    {
                        if (parseMath)
                        {
                            results.Parameters.Add(ParseMathExpression(param));
                        }
                        else if (isComplex)
                        {
                            results.Parameters.Add(ParseMethodCall(param));
                        }
                        else if (param.StartsWith("$") && param.EndsWith("$"))
                        {
                            results.Parameters.Add(new ParsedConstantParameter(param));
                        }
                        else
                        {
                            if (param.Length > 0)
                            {
                                results.Parameters.Add(new ParsedFieldParameter(param));
                            }
                        }

                        parseMath = false;
                        isComplex = false;
                        param = string.Empty;
                    }
                }

                return results;
            }
            else
            {
                //Parse constant.
                return new ParsedConstantParameter(text);
            }
        }

        private static List<PreparseField> PreParseQueryFields(QueryTokenizer query)
        {
            var preparseFields = new List<PreparseField>();

            while (true)
            {
                var param = new StringBuilder();
                var alias = string.Empty;

                int parenScope = 0;
                bool isComplex = false;

                while (true)
                {
                    var token = query.PeekNextToken(new char[] { ',', '(', ')' });

                    if (token != string.Empty && _mathChars.Contains(token[0]) && !(token[0] == '(' || token[0] == ')'))
                    {
                        isComplex = true; //Found math token;
                    }

                    if (query.NextCharacter == '(')
                    {
                        param.Append(query.NextCharacter);
                        query.SkipNextChar();
                        isComplex = true;
                        parenScope++;
                    }
                    else if (query.NextCharacter == ')')
                    {
                        param.Append(query.NextCharacter);
                        query.SkipNextChar();
                        parenScope--;
                    }
                    else if ((query.NextCharacter == ',' && parenScope == 0) || token.ToLower() == "from")
                    {
                        if (parenScope != 0)
                        {
                            throw new KbParserException("Invalid query. Found end of field while still in scope.");
                        }

                        if (param.Length > 0 && char.IsDigit(param[0]))
                        {
                            isComplex = true;
                        }

                        preparseFields.Add(new PreparseField { Text = param.ToString(), Alias = alias, IsComplex = isComplex });

                        //Done with this parameter.
                        query.SkipWhile(',');

                        if (token.ToLower() == "from")
                        {
                            return preparseFields;
                        }

                        isComplex = false;

                        break;
                    }
                    else if (query.NextCharacter == ',')
                    {
                        param.Append(query.NextCharacter);
                        query.SkipWhile(',');
                    }
                    else if (token.ToLower() == "as")
                    {
                        query.SkipNextToken();
                        alias = query.GetNextToken();
                    }
                    else
                    {
                        param.Append(query.GetNextToken());
                    }
                }
            }
        }
    }
}
