using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Parsers.Query;
using System.Text.RegularExpressions;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    /// <summary>
    /// Used to walk various types of string and expressions.
    /// </summary>
    internal class Tokenizer
    {
        #region Rules and Convention.

        /*
         * Public functions that DO NOT modify the internal caret should NOT be prefixed with anything particular.
         * Public functions that DO modify the internal caret should be prefixed with "Eat".
         * Public functions that DO NOT throw exceptions should be prefixed with "Try".
         * Public functions that are NOT prefixed with "Try" should throw exceptions if they do not find/seek what they are intended to do.
        */

        /* Token Placeholders:
         * $n_0$ = numeric.
         * $s_0$ = string.
         * $x_0$ = expression (result from a function call).
         * $f_0$ = document field placeholder.
         */

        #endregion

        #region Private backend variables.

        private string? _hash = null;
        private readonly Stack<int> _breadCrumbs = new();
        private int _literalKey = 0;
        private string _text;
        private int _caret = 0;
        private readonly char[] _standardTokenDelimiters;

        #endregion

        public char? NextCharacter => _caret < _text.Length ? _text[_caret] : null;
        public bool IsExhausted() => _caret >= _text.Length;
        public char[] TokenDelimiters => _standardTokenDelimiters;
        public int Caret => _caret;
        public int Length => _text.Length;
        public string Text => _text;
        public string Hash => _hash ??= Library.Helpers.ComputeSHA256(_text);
        public KbInsensitiveDictionary<KbConstant> PredefinedConstants { get; set; }
        public KbInsensitiveDictionary<QueryFieldLiteral> Literals { get; private set; } = new();

        #region Constructors.

        /// <summary>
        /// Creates a tokenizer which uses only whitespace as a delimiter.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="standardTokenDelimiters">Delimiters to stop on when parsing tokens. If not specified, will only stop on whitespace.</param>
        /// <param name="optimizeForTokenization">Whether the query text should be optimized for tokenization.
        /// This only needs to be done once per query text. For example, if you create another Tokenizer
        /// with a subset of this Tokenizer, then the new instance does not need to be optimized</param>
        public Tokenizer(string text, char[] standardTokenDelimiters, bool optimizeForTokenization = false,
            KbInsensitiveDictionary<KbConstant>? predefinedConstants = null)
        {
            _text = new string(text.ToCharArray());
            _standardTokenDelimiters = standardTokenDelimiters;
            PredefinedConstants = predefinedConstants ?? new();

            PreValidate();

            if (optimizeForTokenization)
            {
                OptimizeForTokenization();
                PostValidate();
            }
        }

        /// <summary>
        /// Creates a tokenizer which uses only whitespace as a delimiter.
        /// </summary>
        /// <param name="text">Text to tokenize.</param>
        /// <param name="optimizeForTokenization">Whether the query text should be optimized for tokenization.
        /// This only needs to be done once per query text. For example, if you create another Tokenizer
        /// with a subset of this Tokenizer, then the new instance does not need to be optimized</param>
        public Tokenizer(string text, bool optimizeForTokenization = false,
            KbInsensitiveDictionary<KbConstant>? predefinedConstants = null)
        {
            _text = new string(text.ToCharArray());
            _standardTokenDelimiters = ['\r', '\n', ' '];
            PredefinedConstants = predefinedConstants ?? new();

            PreValidate();

            if (optimizeForTokenization)
            {
                OptimizeForTokenization();
                PostValidate();
            }
        }

        private void PreValidate()
        {
            ValidateParentheses();
        }

        private void PostValidate()
        {
            int index = _text.IndexOf('\'');
            if (index > 0)
            {
                throw new KbParserException($"Invalid syntax at ['] at position: [{index:n0}]");
            }

            index = _text.IndexOf('\"');
            if (index > 0)
            {
                throw new KbParserException($"Invalid syntax at [\"] at position: [{index:n0}]");
            }
        }

        private void ValidateParentheses()
        {
            int parenOpen = 0;
            int parenClose = 0;

            for (int i = 0; i < _text.Length; i++)
            {
                if (_text[i] == '(')
                {
                    parenOpen++;
                }
                else if (_text[i] == ')')
                {
                    parenClose++;
                }

                if (parenClose > parenOpen)
                {
                    throw new KbParserException($"Parentheses mismatch in expression: [{_text}]");
                }
            }

            if (parenClose != parenOpen)
            {
                throw new KbParserException($"Parentheses mismatch in expression: [{_text}]");
            }
        }

        #endregion

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

        #region Contains.

        /// <summary>
        /// Returns true if the tokenizer text contains the given string.
        /// </summary>
        public bool Contains(string givenString)
            => _text.Contains(givenString, StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// Returns true if the tokenizer text contains any of the the given strings.
        /// </summary>
        public bool Contains(string[] givenStrings)
        {
            foreach (var givenString in givenStrings)
            {
                if (_text.Contains(givenString, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region GetNextIndexOf.

        /// <summary>
        /// Returns the position of the any of the given characters, seeks from the current caret position.
        /// </summary>
        public bool TryGetNextIndexOf(char[] characters, out int foundIndex)
        {
            for (int i = _caret; i < _text.Length; i++)
            {
                if (characters.Contains(_text[i]))
                {
                    foundIndex = i;
                    return true;
                }
            }

            foundIndex = -1;
            return false;
        }

        /// <summary>
        /// Returns the position of the any of the given characters, seeks from the current caret position.
        /// Throws exception if the character is not found.
        /// </summary>
        public int GetNextIndexOf(char[] characters)
        {
            if (TryGetNextIndexOf(characters, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Tokenizer character not found [{string.Join("],[", characters)}].");
        }

        /// <summary>
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public bool TryGetNextIndexOf(string[] givenStrings, out int foundIndex)
        {
            foreach (var givenString in givenStrings)
            {
                int index = _text.IndexOf(givenString, _caret, StringComparison.InvariantCultureIgnoreCase);
                if (index >= 0)
                {
                    foundIndex = index;
                    return true;
                }
            }

            foundIndex = -1;
            return false;
        }

        /// <summary>
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public int GetNextIndexOf(string[] givenStrings)
        {
            if (TryGetNextIndexOf(givenStrings, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Expected string not found [{string.Join("],[", givenStrings)}].");
        }

        /// <summary>
        /// Returns the index of the given string. Throws exception if not found.
        /// </summary>
        public bool TryGetNextIndexOf(string givenString, out int foundIndex)
        {
            int index = _text.IndexOf(givenString, _caret, StringComparison.InvariantCultureIgnoreCase);
            if (index >= 0)
            {
                foundIndex = index;
                return true;
            }
            foundIndex = -1;
            return false;
        }

        /// <summary>
        /// Returns the index of the given string. Throws exception if not found.
        /// </summary>
        public int GetNextIndexOf(string givenString)
        {
            if (TryGetNextIndexOf(givenString, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Expected string not found [{string.Join("],[", givenString)}].");
        }

        public delegate bool GetNextIndexOfProc(string peekedToken);

        /// <summary>
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public int GetNextIndexOf(GetNextIndexOfProc proc)
        {
            if (TryGetNextIndexOf(proc, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Expected string not found {proc.GetType().Name}.");
        }

        /// <summary>
        /// Returns true if the given strings is found.
        /// </summary>
        public bool TryGetNextIndexOf(GetNextIndexOfProc proc, out int foundIndex)
        {
            int restoreCaret = _caret;

            while (IsExhausted() == false)
            {
                int previousCaret = _caret;
                var token = EatGetNext();

                if (proc(token))
                {
                    foundIndex = previousCaret;
                    _caret = restoreCaret;
                    return true;
                }
            }

            foundIndex = -1;
            _caret = restoreCaret;

            return false;
        }

        #endregion

        #region Substring.

        /// <summary>
        /// Gets the remainder of the tokenizer text from the internal caret position to the given absolute position.
        /// </summary>
        public string EatRemainder()
        {
            RecordBreadcrumb();

            var result = _text.Substring(_caret);
            _caret = _text.Length;

            InternalEatWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position for a given length.
        /// </summary>
        public string EatSubString(int length)
        {
            RecordBreadcrumb();

            var result = _text.Substring(_caret, length);
            _caret += length;

            InternalEatWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position to the given absolute position.
        /// </summary>
        public string EatSubStringAbsolute(int absoluteEndPosition)
        {
            RecordBreadcrumb();

            var result = _text.Substring(_caret, absoluteEndPosition - _caret);
            _caret = absoluteEndPosition;

            InternalEatWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position to the given absolute position.
        /// </summary>
        public string EatSubString(int startPosition, int length)
        {
            RecordBreadcrumb();

            var result = _text.Substring(startPosition, length);
            _caret = startPosition + length;

            InternalEatWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets a substring from the tokenizer.
        /// </summary>
        public string SubString(int startPosition, int length)
            => _text.Substring(startPosition, length);

        /// <summary>
        /// Gets the remainder of the text from the given position.
        /// </summary>
        public string RemainderFrom(int startPosition)
            => _text.Substring(startPosition);

        /// <summary>
        /// Gets the remainder of the text from current caret position.
        /// </summary>
        public string Remainder()
            => _text.Substring(_caret);

        #endregion

        #region SkipNext.

        /// <summary>
        /// Skips the next token using the standard delimiters.
        /// </summary>
        public void EatNextToken()
            => EatGetNext(_standardTokenDelimiters, out _);

        /// <summary>
        /// Skips the next token using the given delimiters.
        /// </summary>
        public void EatNextToken(char[] delimiters)
            => EatGetNext(delimiters, out _);

        /// <summary>
        /// Skips the next token using the standard delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter..
        /// </summary>
        public void EatNextToken(out char outStoppedOnDelimiter)
            => EatGetNext(_standardTokenDelimiters, out outStoppedOnDelimiter);

        /// <summary>
        /// Skips the next token using the given delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter..
        /// </summary>
        public void EatNextToken(char[] delimiters, out char outStoppedOnDelimiter)
            => EatGetNext(delimiters, out outStoppedOnDelimiter);

        #endregion

        #region IsNextToken.

        public delegate bool TryNextTokenValidationProc(string peekedToken);

        public delegate bool TryNextTokenComparerProc(string peekedToken, string givenToken);
        public delegate bool TryNextTokenProc(string peekedToken);

        #region TryEatNextTokenEndsWith

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string[] givenTokens, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string[] givenTokens)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string givenToken, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string givenToken)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string[] givenTokens, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string givenToken, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenEndsWith(string givenToken, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        #endregion

        #region TryEatNextTokenStartsWith

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string[] givenTokens, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string[] givenTokens)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string givenToken, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string givenToken)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string[] givenTokens, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string givenToken, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenStartsWith(string givenToken, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        #endregion

        #region TryEatNextTokenContains

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string[] givenTokens, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string[] givenTokens)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string givenToken, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string givenToken)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string[] givenTokens, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string givenToken, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatNextTokenContains(string givenToken, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        #endregion

        #region TryEatValidateNextToken

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNextToken(TryNextTokenValidationProc validator)
            => TryEatValidateNextToken(validator, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNextToken(TryNextTokenValidationProc validator, char[] delimiters)
            => TryEatValidateNextToken(validator, delimiters, out _);

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNextToken(TryNextTokenValidationProc validator, out string outFoundToken)
            => TryEatValidateNextToken(validator, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the given validator function returns true for the next token, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatValidateNextToken(TryNextTokenValidationProc validator, char[] delimiters, out string outFoundToken)
        {
            int restoreCaret = _caret;
            outFoundToken = EatGetNext(delimiters, out _);

            if (validator(outFoundToken))
            {
                return true;
            }

            _caret = restoreCaret;
            return false;
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNextToken(TryNextTokenComparerProc comparer, string[] givenTokens)
            => TryEatCompareNextToken(comparer, givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNextToken(TryNextTokenComparerProc comparer, string[] givenTokens, char[] delimiters)
            => TryEatCompareNextToken(comparer, givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNextToken(TryNextTokenComparerProc comparer, string[] givenTokens, out string outFoundToken)
            => TryEatCompareNextToken(comparer, givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatCompareNextToken(TryNextTokenComparerProc comparer, string[] givenTokens, char[] delimiters, out string outFoundToken)
        {
            int restoreCaret = _caret;
            outFoundToken = EatGetNext(delimiters, out _);

            foreach (var givenToken in givenTokens)
            {
                if (comparer(outFoundToken, givenToken))
                {
                    return true;
                }
            }

            _caret = restoreCaret;
            return false;
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string[] givenTokens, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string[] givenTokens)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string givenToken)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string givenToken, char[] delimiters)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string[] givenTokens, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string givenToken, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// Moves the caret past the token only if its matched.
        /// </summary>
        public bool TryEatIsNextToken(string givenToken, char[] delimiters, out string outFoundToken)
            => TryEatCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        #endregion

        #endregion

        #region IsNextToken.

        #region TryNextTokenEndsWith

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenEndsWith(string[] givenTokens, char[] delimiters)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenEndsWith(string[] givenTokens)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenEndsWith(string givenToken, char[] delimiters)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenEndsWith(string givenToken)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenEndsWith(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenEndsWith(string[] givenTokens, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenEndsWith(string givenToken, char[] delimiters, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenEndsWith(string givenToken, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        #endregion

        #region TryNextTokenStartsWith

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenStartsWith(string[] givenTokens, char[] delimiters)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenStartsWith(string[] givenTokens)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenStartsWith(string givenToken, char[] delimiters)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenStartsWith(string givenToken)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenStartsWith(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenStartsWith(string[] givenTokens, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenStartsWith(string givenToken, char[] delimiters, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenStartsWith(string givenToken, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        #endregion

        #region TryNextTokenContains

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenContains(string[] givenTokens, char[] delimiters)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenContains(string[] givenTokens)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenContains(string givenToken, char[] delimiters)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenContains(string givenToken)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenContains(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenContains(string[] givenTokens, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenContains(string givenToken, char[] delimiters, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryNextTokenContains(string givenToken, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        #endregion

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// </summary>
        public bool TryCompareNextToken(TryNextTokenProc comparer, char[] delimiters)
        {
            var token = GetNext(delimiters);
            return comparer(token);
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// </summary>
        public bool TryCompareNextToken(TryNextTokenProc comparer)
        {
            var token = GetNext();
            return comparer(token);
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryCompareNextToken(TryNextTokenComparerProc comparer, string[] givenTokens, char[] delimiters, out string outFoundToken)
        {
            outFoundToken = GetNext(delimiters);

            foreach (var givenToken in givenTokens)
            {
                if (comparer(outFoundToken, givenToken))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// </summary>
        public bool TryIsNextToken(string[] givenTokens, char[] delimiters)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// </summary>
        public bool TryIsNextToken(string[] givenTokens)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// </summary>
        public bool TryIsNextToken(string givenToken)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out _);

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// </summary>
        public bool TryIsNextToken(string givenToken, char[] delimiters)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out _);

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryIsNextToken(string[] givenTokens, char[] delimiters, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryIsNextToken(string[] givenTokens, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryIsNextToken(string givenToken, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters, out outFoundToken);

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// Regardless of whether a match was made, the token which was parsed it returned via outFoundToken.
        /// </summary>
        public bool TryIsNextToken(string givenToken, char[] delimiters, out string outFoundToken)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters, out outFoundToken);

        #endregion

        #region GetNext.

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        public T EatGetNext<T>()
            => Helpers.Converters.ConvertTo<T>(EatGetNext(_standardTokenDelimiters, out _));

        /// <summary>
        /// Gets the next token using the standard delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter..
        /// </summary>
        public T EatGetNext<T>(out char outStoppedOnDelimiter)
            => Helpers.Converters.ConvertTo<T>(EatGetNext(_standardTokenDelimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters.
        /// </summary>
        public T? EatGetNextEvaluated<T>(char[] delimiters)
            => Helpers.Converters.ConvertToNullable<T>(EatGetNextEvaluated(delimiters));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters.
        /// </summary>
        public T? EatGetNextEvaluated<T>()
            => Helpers.Converters.ConvertToNullable<T>(EatGetNextEvaluated(_standardTokenDelimiters));

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        public T EatGetNext<T>(char[] delimiters)
            => Helpers.Converters.ConvertTo<T>(EatGetNext(delimiters, out _));

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        public string EatGetNext()
            => EatGetNext(_standardTokenDelimiters, out _);

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters.
        /// </summary>
        public string? EatGetNextEvaluated()
            => ResolveLiteral(EatGetNext(_standardTokenDelimiters, out _));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters.
        /// </summary>
        public string? EatGetNextEvaluated(char[] delimiters)
            => ResolveLiteral(EatGetNext(delimiters, out _));

        /// <summary>
        /// Gets the next token using the given delimiters,
        /// returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public T EatGetNext<T>(char[] delimiters, out char outStoppedOnDelimiter)
            => Helpers.Converters.ConvertTo<T>(EatGetNext(delimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token using the standard delimiters,
        /// returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string EatGetNext(out char outStoppedOnDelimiter)
            => EatGetNext(_standardTokenDelimiters, out outStoppedOnDelimiter);

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string? EatGetNextEvaluated(out char outStoppedOnDelimiter)
            => ResolveLiteral(EatGetNext(_standardTokenDelimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string? EatGetNextEvaluated(char[] delimiters, out char outStoppedOnDelimiter)
            => ResolveLiteral(EatGetNext(delimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        public string EatGetNext(char[] delimiters)
            => EatGetNext(delimiters, out _);

        /// <summary>
        /// Gets the next token using the given delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string EatGetNext(char[] delimiters, out char outStoppedOnDelimiter)
        {
            RecordBreadcrumb();

            outStoppedOnDelimiter = '\0';

            var token = string.Empty;

            if (_caret == _text.Length)
            {
                return string.Empty;
            }

            for (; _caret < _text.Length; _caret++)
            {
                if (delimiters.Contains(_text[_caret]) == true)
                {
                    outStoppedOnDelimiter = _text[_caret];
                    _caret++; //skip the delimiter.
                    break;
                }

                token += _text[_caret];
            }

            InternalEatWhiteSpace();

            return token.Trim();
        }

        #endregion

        #region GetNext.

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        public T GetNext<T>()
            => Helpers.Converters.ConvertTo<T>(GetNext(_standardTokenDelimiters));

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        public T GetNext<T>(char[] delimiters)
            => Helpers.Converters.ConvertTo<T>(GetNext(delimiters));

        /// <summary>
        /// Returns the next token without moving the caret.
        /// </summary>
        public string GetNext()
            => GetNext(_standardTokenDelimiters);

        /// <summary>
        /// Returns the next token without moving the caret using the given delimiters.
        /// </summary>
        public string GetNext(char[] delimiters)
        {
            int restoreCaret = _caret;
            var token = EatGetNext(delimiters, out _);
            _caret = restoreCaret;
            return token;
        }

        /// <summary>
        /// Returns the next token without moving the caret using the given delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter..
        /// </summary>
        public string GetNext(char[] delimiters, out char outStoppedOnDelimiter)
        {
            int restoreCaret = _caret;
            var token = EatGetNext(delimiters, out outStoppedOnDelimiter);
            _caret = restoreCaret;
            return token;
        }

        #endregion

        #region IsNextNonIdentifier.

        /// <summary>
        /// Returns true if the next character that is not a letter/digit/whitespace is in the given array.
        /// </summary>
        public bool IsNextNonIdentifier(char[] c)
            => IsNextNonIdentifier(c, out _);

        /// <summary>
        /// Returns true if the next character that is not a letter/digit/whitespace is in the given array.
        /// </summary>
        public bool IsNextNonIdentifier(char[] c, out int index)
        {
            for (int i = _caret; i < _text.Length; i++)
            {
                if (_text[i].IsQueryIdentifier())
                {
                    continue;
                }
                else if (c.Contains(_text[i]))
                {
                    index = i;
                    return true;
                }
                else
                {
                    index = -1;
                    return false;
                }
            }

            index = -1;
            return false;
        }

        #endregion

        #region IsNextCharacter.

        public delegate bool NextCharacterProc(char c);

        /// <summary>
        /// Returns the boolean value from the given delegate which is passed the next character in the sequence.
        /// </summary>
        public bool IsNextCharacter(NextCharacterProc proc)
        {
            var next = NextCharacter ?? throw new KbParserException("The tokenizer sequence is empty.");
            return proc(next);
        }

        /// <summary>
        /// Returns true if the next character matches the given value.
        /// </summary>
        public bool IsNextCharacter(char character)
            => NextCharacter == character;

        public bool TryEatIsNextCharacter(char character)
        {
            RecordBreadcrumb();
            if (NextCharacter == character)
            {
                _caret++;
                InternalEatWhiteSpace();
                return true;
            }
            return false;
        }

        #endregion

        #region GetMatchingScope.

        /// <summary>
        /// Matches scope using open and close parentheses and skips the entire scope.
        /// </summary>
        public void EatMatchingScope()
        {
            EatGetMatchingScope('(', ')');
        }

        /// <summary>
        /// Matches scope using the given open and close values and skips the entire scope.
        /// </summary>
        public void EatMatchingScope(char open, char close)
        {
            EatGetMatchingScope(open, close);
        }

        /// <summary>
        /// Matches scope using open and close parentheses and returns the text between them.
        /// </summary>
        public string EatGetMatchingScope()
        {
            return EatGetMatchingScope('(', ')');
        }

        /// <summary>
        /// Matches scope using the given open and close values and returns the text between them.
        /// </summary>
        public string EatGetMatchingScope(char open, char close)
        {
            RecordBreadcrumb();

            int scope = 0;

            InternalEatWhiteSpace();

            if (_text[_caret] != open)
            {
                throw new Exception($"Expected scope character not found [{open}].");
            }

            int startPosition = _caret + 1;

            for (; _caret < _text.Length; _caret++)
            {
                if (_text[_caret] == open)
                {
                    scope++;
                }
                else if (_text[_caret] == close)
                {
                    scope--;
                }

                if (scope < 0)
                {
                    throw new Exception($"Expected scope [{open}] and [{close}] fell below zero.");
                }

                if (scope == 0)
                {
                    var result = _text.Substring(startPosition, _caret - startPosition).Trim();

                    _caret++;
                    InternalEatWhiteSpace();

                    return result;
                }
            }

            throw new Exception($"Expected matching scope not found [{open}] and [{close}], ended at scope [{scope}].");
        }

        #endregion

        #region Skip / Eat.

        /// <summary>
        /// Moves the caret forward while the character is in the given list, returns the count of skipped.
        /// </summary>
        public int EatWhile(char[] characters)
        {
            int count = 0;
            while (_caret < _text.Length && characters.Contains(_text[_caret]))
            {
                count++;
                _caret++;
            }
            InternalEatWhiteSpace();
            return count;
        }

        /// <summary>
        /// Moves the caret forward while the character matches the given value, returns the count of skipped.
        /// </summary>
        public int EatWhile(char character)
            => EatWhile([character]);

        /// <summary>
        /// Moves the caret forward by one character (then whitespace) if the character is in the given list, returns true if match was found.
        /// </summary>
        public bool EatIf(char[] characters)
        {
            if (_caret < _text.Length && characters.Contains(_text[_caret]))
            {
                _caret++;
                InternalEatWhiteSpace();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Moves the caret forward by one character (then whitespace) if the character matches the given value, returns true if match was found.
        /// </summary>
        public bool EatIf(char character)
            => EatIf([character]);

        /// <summary>
        /// Moves the caret past any whitespace, does not record breadcrumb.
        /// </summary>
        private void InternalEatWhiteSpace()
        {
            while (_caret < _text.Length && char.IsWhiteSpace(_text[_caret]))
            {
                _caret++;
            }
        }

        /// <summary>
        /// Moves the caret past any whitespace.
        /// </summary>
        public void EatWhiteSpace()
        {
            RecordBreadcrumb();

            while (_caret < _text.Length && char.IsWhiteSpace(_text[_caret]))
            {
                _caret++;
            }
        }

        /// <summary>
        /// Skips the next character in the sequence.
        /// </summary>
        /// <exception cref="KbParserException"></exception>
        public void EatNextCharacter()
        {
            RecordBreadcrumb();

            if (_caret >= _text.Length)
            {
                throw new KbParserException("The tokenizer sequence is empty.");
            }

            _caret++;
            InternalEatWhiteSpace();
        }

        #endregion

        #region Caret Operations.

        public void SetText(string text, int caret)
        {
            _text = text;
            _caret = caret;
            if (_caret >= _text.Length)
            {
                throw new KbParserException("Caret position is greater than text length.");
            }
        }

        public void SetText(string text)
        {
            _text = text;
            if (_caret >= _text.Length)
            {
                throw new KbParserException("Caret position is greater than text length.");
            }
        }

        private void RecordBreadcrumb()
        {
            if (_breadCrumbs.Count == 0 || _breadCrumbs.Peek() != _caret)
            {
                _breadCrumbs.Push(_caret);
            }
        }

        /// <summary>
        /// Places the caret back to the beginning.
        /// </summary>
        public void Rewind()
        {
            _caret = 0;
            _breadCrumbs.Clear();
        }

        /// <summary>
        /// Returns the position of the caret before the previous tokenization operation.
        /// </summary>
        /// <returns></returns>
        public int PreviousCaret()
        {
            if (_breadCrumbs.Count == 0)
            {
                throw new KbParserException("Tokenization steps are out of range.");
            }

            return _breadCrumbs.Peek();
        }

        /// <summary>
        /// Sets the caret to where it was before the previous tokenization operation.
        /// </summary>
        /// <returns></returns>
        public void StepBack()
        {
            if (_breadCrumbs.Count == 0)
            {
                throw new KbParserException("Tokenization steps are out of range.");
            }
            _caret = _breadCrumbs.Pop();
            throw new KbParserException("Tokenization steps are out of range.");
        }

        /// <summary>
        /// Sets the caret to where it was before the previous n-tokenization operations.
        /// </summary>
        /// <returns></returns>
        public void StepBack(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_breadCrumbs.Count == 0)
                {
                    throw new KbParserException("Tokenization steps are out of range.");
                }
                _caret = _breadCrumbs.Pop();
            }
        }

        public void SetCaret(int position)
        {
            if (position > _text.Length)
            {
                throw new KbParserException("Tokenization caret moved past end of text.");
            }
            else if (position < 0)
            {
                throw new KbParserException("Tokenization caret moved past beginning of text.");
            }
            _caret = position;
        }

        #endregion
    }
}
