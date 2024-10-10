using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Exceptions;
using System.Text;
using System.Text.RegularExpressions;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        #region Swap in/out literals.

        /// <summary>
        /// Attempts to resolve a single string or numeric literal, otherwise returns the given value.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public string? ResolveLiteral(string token)
        {
            if (Literals.TryGetValue(token, out var literal))
            {
                return literal.Value;
            }
            return token;
        }

        /// <summary>
        /// Replaces text literals with tokens to prepare the query for parsing.
        /// </summary>
        private void SwapOutStringLiterals(ref string query)
        {
            //Literal strings.
            var regex = new Regex("\"([^\"\\\\]*(\\\\.[^\"\\\\]*)*)\"|\\'([^\\'\\\\]*(\\\\.[^\\'\\\\]*)*)\\'");
            while (true)
            {
                var match = regex.Match(query);

                if (match.Success)
                {
                    string key = $"$s_{_literalKey++}$";
                    Literals.Add(key, new(KbBasicDataType.String, match.ToString()[1..^1]));

                    query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                }
                else
                {
                    break;
                }
            }

            if (PredefinedConstants.Count > 0)
            {
                var triedConstants = new Dictionary<string, string>();
                int nextTriedConstant = 0;

                //Predefined string constants.
                //regex = new Regex(@"(?<=\s|^)[A-Za-z_][A-Za-z0-9_]*(?=\s|$)|(?<=\s|^)@\w+(?=\s|$)");
                regex = new Regex(@"(?<=\s|^)[A-Za-z_][A-Za-z0-9_]*(?=\s|$)|(?<=\s|^)@\w+");
                while (true)
                {
                    var match = regex.Match(query);

                    if (match.Success)
                    {
                        if (match.Value.StartsWith('@'))
                        {
                            //This is a variable, and unlike a constant - we require them to be declared.

                            if (PredefinedConstants.TryGetValue(match.ToString(), out var variable))
                            {
                                if (variable.DataType == KbBasicDataType.String)
                                {
                                    string key = $"$s_{_literalKey++}$";
                                    Literals.Add(key, new(KbBasicDataType.String, variable.Value));
                                    query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                                }
                                else
                                {
                                    //Keep track of "constants" that we do not have definitions for, we will need replace these.
                                    string key = $"$temp_const_{nextTriedConstant++}$";
                                    query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                                    triedConstants.Add(key, match.ToString());
                                }
                            }
                            else
                            {
                                throw new KbParserException(GetCurrentLineNumber(), $"Variable not defined: [{match}].");
                            }
                        }
                        else if (PredefinedConstants.TryGetValue(match.ToString(), out var constant))
                        {
                            if (constant.DataType == KbBasicDataType.String)
                            {
                                string key = $"$s_{_literalKey++}$";
                                Literals.Add(key, new(KbBasicDataType.String, constant.Value));
                                query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                            }
                            else
                            {
                                //Keep track of "constants" that we do not have definitions for, we will need replace these.
                                string key = $"$temp_const_{nextTriedConstant++}$";
                                query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                                triedConstants.Add(key, match.ToString());
                            }
                        }
                        else
                        {
                            //Keep track of "constants" that we do not have definitions for, we will need replace these.
                            string key = $"$temp_const_{nextTriedConstant++}$";
                            query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                            triedConstants.Add(key, match.ToString());
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                //Replace the "constants" that were not defined.
                foreach (var triedConstant in triedConstants)
                {
                    query = query.Replace(triedConstant.Key, triedConstant.Value);
                }
            }
        }

        /// <summary>
        /// Replaces numeric literals with tokens to prepare the query for parsing.
        /// </summary>
        private void SwapOutNumericLiterals(ref string query)
        {
            //Literal numeric:
            var regex = new Regex(@"(?<=\s|^)-?(?:\d+\.?\d*|\.\d+)(?=\s|$)");
            while (true)
            {
                var match = regex.Match(query);

                if (match.Success)
                {
                    string key = $"$n_{_literalKey++}$";
                    Literals.Add(key, new(KbBasicDataType.Numeric, match.ToString()));
                    query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                }
                else
                {
                    break;
                }
            }

            if (PredefinedConstants.Count > 0)
            {
                var triedConstants = new Dictionary<string, string>();
                int nextTriedConstant = 0;

                //Predefined numeric constants.
                regex = new Regex(@"(?<=\s|^)[A-Za-z_][A-Za-z0-9_]*(?=\s|$)|(?<=\s|^)@\w+(?=\s|$)");
                while (true)
                {
                    var match = regex.Match(query);

                    if (match.Success)
                    {
                        if (PredefinedConstants.TryGetValue(match.ToString(), out var constant))
                        {
                            if (match.Value.StartsWith('@'))
                            {
                                //This is a variable, and unlike a constant - we require them to be declared.
                                if (PredefinedConstants.TryGetValue(match.ToString(), out var variable))
                                {
                                    if (variable.DataType == KbBasicDataType.Numeric)
                                    {
                                        string key = $"$s_{_literalKey++}$";
                                        Literals.Add(key, new(KbBasicDataType.Numeric, variable.Value));
                                        query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                                    }
                                    else
                                    {
                                        //Keep track of "constants" that we do not have definitions for, we will need replace these.
                                        string key = $"$temp_const_{nextTriedConstant++}$";
                                        query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                                        triedConstants.Add(key, match.ToString());
                                    }
                                }
                                else
                                {
                                    throw new KbParserException(GetCurrentLineNumber(), $"Variable not defined: [{match}].");
                                }
                            }
                            else if (constant.DataType == KbBasicDataType.Numeric)
                            {
                                string key = $"$n_{_literalKey++}$";
                                Literals.Add(key, new(KbBasicDataType.Numeric, constant.Value));
                                query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                            }
                            else
                            {
                                //Keep track of "constants" that we do not have definitions for, we will need replace these.
                                string key = $"$temp_const_{nextTriedConstant++}$";
                                query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                                triedConstants.Add(key, match.ToString());
                            }
                        }
                        else
                        {
                            //Keep track of "constants" that we do not have definitions for, we will need replace these.
                            string key = $"$temp_const_{nextTriedConstant++}$";
                            query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                            triedConstants.Add(key, match.ToString());
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                //Replace the "constants" that were not defined.
                foreach (var triedConstant in triedConstants)
                {
                    query = query.Replace(triedConstant.Key, triedConstant.Value);
                }
            }
        }

        #endregion

        # region Clean query text.

        /// <summary>
        /// Removes all unnecessary whitespace, newlines, comments and replaces literals with tokens to prepare query for parsing.
        /// </summary>
        private void OptimizeForTokenization()
        {
            string text = KbTextUtility.RemoveNonCode(_text);

            //var maxNumericLiterals = NumericLiterals.Count > 0 ? NumericLiterals.Max(o => o.Key)?.Substring(2)?.TrimEnd(['$']) : "0";
            //var maxStringLiterals = StringLiterals.Count > 0 ? StringLiterals.Max(o => o.Key)?.Substring(2)?.TrimEnd(['$']) : "0";

            SwapOutStringLiterals(ref text);

            var expandCharacters = new List<char>();
            expandCharacters.AddRange(TokenizerExtensions.MathematicalCharacters);
            expandCharacters.AddRange(TokenizerExtensions.TokenConnectorCharacters);
            expandCharacters.Add(',');
            expandCharacters.RemoveAll(o => o == '-'); //Because of negative numbers, we have to handle the minus sign separately.

            //We replace numeric constants and we want to make sure we have 
            //  no numbers next to any conditional operators before we do so.
            text = text.Replace("!=", "$$NotEqual$$");
            text = text.Replace(">=", "$$GreaterOrEqual$$");
            text = text.Replace("<=", "$$LesserOrEqual$$");
            text = text.Replace("||", "$$Or$$");
            text = text.Replace("&&", "$$And$$");
            text = text.Replace(".*", "$$StarSchema$$"); //Schema prefixed "select *".

            foreach (var ch in expandCharacters.Distinct())
            {
                text = text.Replace($"{ch}", $" {ch} ");
            }

            text = text.Replace("$$NotEqual$$", " != ");
            text = text.Replace("$$GreaterOrEqual$$", " >= ");
            text = text.Replace("$$LesserOrEqual$$", " <= ");
            text = text.Replace("$$Or$$", " || ");
            text = text.Replace("$$And$$", " && ");
            text = text.Replace("$$StarSchema$$", ".*");

            //Pad spaces around '-' where the right hand side is not a digit.
            for (int i = 0; i < text.Length; i++)
            {
                if ((i = text.IndexOf('-', i)) < 0)
                {
                    break;
                }

                //If the character after the minus-sign is not a number, then pad it as usual.
                if (!(i < text.Length - 1 && char.IsDigit(text[i + 1])))
                {
                    text = Helpers.Text.ReplaceRange(text, i, 1, " - ");
                    i += 1;
                }
            }

            SwapOutNumericLiterals(ref text);

            int length;
            do
            {
                length = text.Length;
                text = text.Replace("\t", " ");
                text = text.Replace("  ", " ");
            }
            while (length != text.Length);

            //text = text.Trim();

            text = text.Replace("(", " ( ").Replace(")", " ) ");

            KbTextUtility.RemoveNonCode(text);

            text = CleanLinesAndRecordLineRanges(text);

            //We add a single whitespace at the end so we can match whitespace
            //  padded string such as " text " even when they are the last word.
            _text = text.Trim() + ' ';
            _length = _text.Length;
        }

        private string CleanLinesAndRecordLineRanges(string query)
        {
            var result = new StringBuilder();

            query = query.Replace("\r\n", "\n");

            int lineNumber = 0;

            foreach (var line in query.Split('\n'))
            {
                lineNumber++;

                string cleanedLine = RemoveDoubleWhitespace(line.Trim()) + ' ';

                if (string.IsNullOrWhiteSpace(cleanedLine.Trim()) == false)
                {
                    int startIndex = result.Length;
                    result.Append(cleanedLine);
                    LineRanges.Add(new(lineNumber, startIndex, result.Length));
                }
            }

            return result.ToString();
        }

        public static string RemoveDoubleWhitespace(string query)
            => Regex.Replace(query, @"\s+", " ");

        #endregion
    }
}
