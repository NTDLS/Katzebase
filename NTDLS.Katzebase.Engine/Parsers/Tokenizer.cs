using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using System.Text.RegularExpressions;

namespace NTDLS.Katzebase.Engine.Parsers
{
    /// <summary>
    /// Used to walk various types of string and expressions.
    /// </summary>
    public class Tokenizer
    {
        private Stack<int> _breadCrumbs = new();

        #region Rules and Convention.
        /*
         * Public functions that DO NOT modify the internal caret should be prefixed with "Inert".
         * Public functions that DO NOT throw exceptions should be prefixed with "Try".
         * Public functions that are not prefixed with "Try" should throw exceptions if they do not find/seek what they are intended to do.
         * Public functions that DO NOT modify the internal caret and DO NOT throw exceptions should be prefixed with "InertTry".
         */
        #endregion

        #region Private backend variables.

        private string _text;
        private int _caret = 0;
        private readonly char[] _standardTokenDelimiters;
        private KbInsensitiveDictionary<string?>? _userParameters = null;

        #endregion

        #region Public properties.

        public char? NextCharacter => _caret < _text.Length ? _text[_caret] : null;
        public bool IsEnd() => _caret >= _text.Length;
        public char[] TokenDelimiters => _standardTokenDelimiters;
        public int CaretPosition => _caret;

        #endregion

        #region Constructors.

        /// <summary>
        /// Creates a tokenizer which uses only whitespace as a delimiter.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="standardTokenDelimiters">Delimiters to stop on when parsing tokens. If not specified, will only stop on whitespace.</param>
        /// <param name="optimizeForTokenization">Whether the query text should be optimized for tokenization.
        /// This only needs to be done once per query text. For example, if you create another Tokenizer
        /// with a subset of this Tokenizer, then the new instance does not need to be optimized</param>
        public Tokenizer(string text, char[] standardTokenDelimiters, bool optimizeForTokenization = false)
        {
            _text = new string(text.ToCharArray());
            _standardTokenDelimiters = standardTokenDelimiters;

            ValidateParentheses();

            if (optimizeForTokenization)
            {
                OptimizeForTokenization();
            }
        }

        /// <summary>
        /// Creates a tokenizer which uses only whitespace as a delimiter.
        /// </summary>
        /// <param name="text">Text to tokenize.</param>
        /// <param name="optimizeForTokenization">Whether the query text should be optimized for tokenization.
        /// This only needs to be done once per query text. For example, if you create another Tokenizer
        /// with a subset of this Tokenizer, then the new instance does not need to be optimized</param>
        public Tokenizer(string text, bool optimizeForTokenization = false)
        {
            _text = new string(text.ToCharArray());
            _standardTokenDelimiters = Array.Empty<char>();
            ValidateParentheses();

            if (optimizeForTokenization)
            {
                OptimizeForTokenization();
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

        public void SetUserParameters(KbInsensitiveDictionary<string?>? userParameters)
        {
            _userParameters = userParameters;
        }

        #region Swap in/out literals.

        public KbInsensitiveDictionary<string> StringLiterals { get; private set; } = new();
        public KbInsensitiveDictionary<string> NumericLiterals { get; private set; } = new();

        /// <summary>
        /// Replaces text literals with tokens to prepare the query for parsing.
        /// </summary>
        private static KbInsensitiveDictionary<string> SwapOutStringLiterals(ref string query)
        {
            var mappings = new KbInsensitiveDictionary<string>();
            var regex = new Regex("\"([^\"\\\\]*(\\\\.[^\"\\\\]*)*)\"|\\'([^\\'\\\\]*(\\\\.[^\\'\\\\]*)*)\\'");
            int literalKey = 0;

            while (true)
            {
                var match = regex.Match(query);

                if (match.Success)
                {
                    string key = $"$s_{literalKey++}$";
                    mappings.Add(key, match.ToString());

                    query = NTDLS.Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                }
                else
                {
                    break;
                }
            }

            return mappings;
        }

        /// <summary>
        /// Replaces numeric literals with tokens to prepare the query for parsing.
        /// </summary>
        private static KbInsensitiveDictionary<string> SwapOutNumericLiterals(ref string query)
        {
            var mappings = new KbInsensitiveDictionary<string>();
            var regex = new Regex(@"(?<=\s|^)(?:\d+\.?\d*|\.\d+)(?=\s|$)");
            int literalKey = 0;

            while (true)
            {
                var match = regex.Match(query);

                if (match.Success)
                {
                    string key = $"$n_{literalKey++}$";
                    mappings.Add(key, match.ToString());

                    query = NTDLS.Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                }
                else
                {
                    break;
                }
            }

            return mappings;
        }

        #endregion

        # region Clean query text.

        /// <summary>
        /// Removes all unnecessary whitespace, newlines, comments and replaces literals with tokens to prepare query for parsing.
        /// </summary>
        private void OptimizeForTokenization()
        {
            string text = KbTextUtility.RemoveComments(_text);

            StringLiterals = SwapOutStringLiterals(ref text);

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

            NumericLiterals = SwapOutNumericLiterals(ref text);

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
        public bool InertContains(string givenString)
            => _text.Contains(givenString, StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// Returns true if the tokenizer text contains any of the the given strings.
        /// </summary>
        public bool InertContains(string[] givenStrings)
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
        /// Returns the position of the any of the given characters, seeks from the current internal position.
        /// </summary>
        public bool InertTryGetNextIndexOf(char[] characters, out int position)
        {
            int restorePosition = _caret;

            for (int i = _caret; i < _text.Length; i++)
            {
                if (characters.Contains(_text[i]))
                {
                    position = i;
                    return true;
                }
            }

            position = -1;
            return false;
        }

        /// <summary>
        /// Returns the position of the any of the given characters, seeks from the current internal position.
        /// Throws exception if the character is not found.
        /// </summary>
        public int InertGetNextIndexOf(char[] characters)
        {
            for (int i = _caret; i < _text.Length; i++)
            {
                if (characters.Contains(_text[i]))
                {
                    int index = _caret;
                    return index;
                }
            }

            throw new Exception($"Tokenizer character not found [{string.Join("],[", characters)}].");
        }

        /// <summary>
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public int InertGetNextIndexOf(string[] givenStrings)
        {
            foreach (var givenString in givenStrings)
            {
                int index = _text.IndexOf(givenString, _caret, StringComparison.InvariantCultureIgnoreCase);
                if (index >= 0)
                {
                    return index;
                }
            }

            throw new Exception($"Expected string not found [{string.Join("],[", givenStrings)}].");
        }

        /// <summary>
        /// Returns the index of the given string. Throws exception if not found.
        /// </summary>
        public int InertGetNextIndexOf(string givenString)
        {
            int index = _text.IndexOf(givenString, _caret, StringComparison.InvariantCultureIgnoreCase);
            if (index >= 0)
            {
                return index;
            }


            throw new Exception($"Expected string not found [{string.Join("],[", givenString)}].");
        }

        #endregion

        #region Substring.

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position to the given absolute position.
        /// </summary>
        public string SubString(int absoluteEndPosition)
        {
            RecordBreadcrumb();

            var result = _text.Substring(_caret, absoluteEndPosition - _caret);
            _caret = absoluteEndPosition;
            return result;
        }

        /// <summary>
        /// Gets a substring from the tokenizer.
        /// </summary>
        public string InertSubString(int startPosition, int length)
            => _text.Substring(startPosition, length);

        /// <summary>
        /// Gets a substring from the tokenizer.
        /// </summary>
        public string InertSubString(int startPosition)
            => _text.Substring(startPosition);

        #endregion

        #region SkipNext.

        /// <summary>
        /// Skips the next token using the standard delimiters.
        /// </summary>
        public void SkipNext()
            => GetNext(_standardTokenDelimiters);

        /// <summary>
        /// Skips the next token using the given delimiters.
        /// </summary>
        public void SkipNext(char[] delimiters)
            => GetNext(delimiters);

        #endregion

        #region IsNextToken.

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// </summary>
        public bool TryIsNextToken(string[] givenTokens, char[] delimiters)
        {
            int restoreCaret = _caret;
            var token = GetNext(delimiters);

            foreach (var givenToken in givenTokens)
            {
                if (token.Equals(givenToken, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            _caret = restoreCaret;
            return false;
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// </summary>
        public bool TryIsNextToken(string[] givenTokens)
            => TryIsNextToken(givenTokens, _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// </summary>
        public bool TryIsNextToken(string givenToken)
            => TryIsNextToken([givenToken], _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// </summary>
        public bool TryIsNextToken(string givenToken, char[] delimiters)
            => TryIsNextToken([givenToken], delimiters);

        #endregion

        #region GetNext.

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        public string GetNext()
            => GetNext(_standardTokenDelimiters);

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        public string GetNext(char[] delimiters)
        {
            RecordBreadcrumb();

            var token = string.Empty;

            if (_caret == _text.Length)
            {
                return string.Empty;
            }

            for (; _caret < _text.Length; _caret++)
            {
                if (char.IsWhiteSpace(_text[_caret]) || delimiters.Contains(_text[_caret]) == true)
                {
                    break;
                }

                token += _text[_caret];
            }

            InternalSkipWhiteSpace();

            if (token.Length == 0)
            {
                throw new KbParserException("The tokenizer sequence is empty.");
            }

            return token.Trim();
        }

        /// <summary>
        /// Returns the next token without moving the caret.
        /// </summary>
        public string InertGetNext()
                => InertGetNext(_standardTokenDelimiters);

        /// <summary>
        /// Returns the next token without moving the caret.
        /// </summary>
        public string InertGetNext(char[] delimiters)
        {
            int restoreCaret = _caret;
            var token = GetNext(delimiters);
            _caret = restoreCaret;
            return token;
        }

        #endregion

        #region IsNextNonIdentifier.

        /// <summary>
        /// Returns true if the next character that is not a letter/digit/whitespace is in the given array.
        /// </summary>
        public bool InertIsNextNonIdentifier(char[] c)
            => InertIsNextNonIdentifier(c, out _);

        /// <summary>
        /// Returns true if the next character that is not a letter/digit/whitespace is in the given array.
        /// </summary>
        public bool InertIsNextNonIdentifier(char[] c, out int index)
        {
            for (int i = _caret; i < _text.Length; i++)
            {
                if (_text[i].IsIdentifier())
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
        public bool InertIsNextCharacter(NextCharacterProc proc)
        {
            var next = NextCharacter;
            if (next == null)
            {
                throw new KbParserException("The tokenizer sequence is empty.");
            }
            return proc((char)next);
        }

        /// <summary>
        /// Returns true if the next character matches the given value.
        /// </summary>
        public bool InertIsNextCharacter(char c)
            => NextCharacter == c;

        #endregion

        #region GetMatchingBraces.

        /// <summary>
        /// Matches scope using open and close parentheses and returns the text between them.
        /// </summary>
        public string GetMatchingBraces()
        {
            return GetMatchingBraces('(', ')');
        }

        /// <summary>
        /// Matches scope using the given open and close values and returns the text between them.
        /// </summary>
        public string GetMatchingBraces(char open, char close)
        {
            RecordBreadcrumb();

            int scope = 0;

            InternalSkipWhiteSpace();

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
                    InternalSkipWhiteSpace();

                    return result;
                }
            }

            throw new Exception($"Expected matching scope not found [{open}] and [{close}], ended at scope [{scope}].");
        }

        #endregion

        #region Skip / Eat.

        /// <summary>
        /// Moves the caret past any whitespace, does not record breadcrumb.
        /// </summary>
        private void InternalSkipWhiteSpace()
        {
            while (_caret < _text.Length && char.IsWhiteSpace(_text[_caret]))
            {
                _caret++;
            }
        }

        /// <summary>
        /// Moves the caret past any whitespace.
        /// </summary>
        public void SkipWhiteSpace()
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
        public void SkipNextCharacter()
        {
            RecordBreadcrumb();

            if (_caret >= _text.Length)
            {
                throw new KbParserException("The tokenizer sequence is empty.");
            }

            _caret++;
            InternalSkipWhiteSpace();
        }

        #endregion

        #region Caret Operations.

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

        #endregion
    }
}
