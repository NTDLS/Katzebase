using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using System.Text.RegularExpressions;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
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
                regex = new Regex(@"(?<=\s|^)[A-Za-z_][A-Za-z0-9_]*(?=\s|$)|(?<=\s|^)@\w+(?=\s|$)");
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
                                throw new KbParserException($"Variable [{match}] is not defined.");
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
            var regex = new Regex(@"(?<=\s|^)(?:\d+\.?\d*|\.\d+)(?=\s|$)");
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
                                    throw new KbParserException($"Variable [{match}] is not defined.");
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
            string text = KbTextUtility.RemoveComments(_text);

            //var maxNumericLiterals = NumericLiterals.Count > 0 ? NumericLiterals.Max(o => o.Key)?.Substring(2)?.TrimEnd(['$']) : "0";
            //var maxStringLiterals = StringLiterals.Count > 0 ? StringLiterals.Max(o => o.Key)?.Substring(2)?.TrimEnd(['$']) : "0";

            SwapOutStringLiterals(ref text);

            //We replace numeric constants and we want to make sure we have 
            //  no numbers next to any conditional operators before we do so.
            text = text.Replace("!=", "$$NotEqual$$");
            text = text.Replace(">=", "$$GreaterOrEqual$$");
            text = text.Replace("<=", "$$LesserOrEqual$$");
            text = text.Replace("(", " ( ");
            text = text.Replace(")", " ) ");
            text = text.Replace(",", " , ");
            text = text.Replace(">", " > ");
            text = text.Replace("<", " < ");
            text = text.Replace("=", " = ");
            text = text.Replace("$$NotEqual$$", " != ");
            text = text.Replace("$$GreaterOrEqual$$", " >= ");
            text = text.Replace("$$LesserOrEqual$$", " <= ");
            text = text.Replace("||", " || ");
            text = text.Replace("&&", " && ");

            SwapOutNumericLiterals(ref text);

            int length;
            do
            {
                length = text.Length;
                text = text.Replace("\t", " ");
                text = text.Replace("  ", " ");
            }
            while (length != text.Length);

            text = text.Trim();

            text = text.Replace("(", " ( ").Replace(")", " ) ");

            RemoveComments(ref text);

            TrimAllLines(ref text);
            RemoveEmptyLines(ref text);
            RemoveNewlines(ref text);
            RemoveDoubleWhitespace(ref text);

            _text = text.Trim();
        }

        public static void RemoveComments(ref string query)
        {
            query = "\r\n" + query + "\r\n";

            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"--(.*?)\r?\n";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";

            query = Regex.Replace(query,
                blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                me =>
                {
                    if (me.Value.StartsWith("/*") || me.Value.StartsWith("--"))
                        return me.Value.StartsWith("--") ? Environment.NewLine : "";
                    // Keep the literal strings
                    return me.Value;
                },
                RegexOptions.Singleline);
        }

        public static void RemoveEmptyLines(ref string query)
            => query = Regex.Replace(query, @"^\s+$[\r\n]*", "", RegexOptions.Multiline);

        public static void TrimAllLines(ref string query)
            => query = string.Join("\r\n", query.Split('\n').Select(o => o.Trim()));

        public static void RemoveDoubleWhitespace(ref string query)
            => query = Regex.Replace(query, @"\s+", " ");

        public static void RemoveNewlines(ref string query)
            => query = query.Replace("\r\n", " ");

        #endregion
    }
}
