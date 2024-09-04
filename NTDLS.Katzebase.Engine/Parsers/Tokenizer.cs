using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Parsers.Query;
using System.Text.RegularExpressions;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers
{
    /// <summary>
    /// Used to walk various types of string and expressions.
    /// </summary>
    internal class Tokenizer
    {
        #region Rules and Convention.

        /*
         * Public functions that DO NOT modify the internal caret should be prefixed with "Inert".
         * Public functions that DO NOT throw exceptions should be prefixed with "Try".
         * Public functions that are not prefixed with "Try" should throw exceptions if they do not find/seek what they are intended to do.
         * Public functions that DO NOT modify the internal caret and DO NOT throw exceptions should be prefixed with "InertTry".
        */

        /* Token Placeholders:
         * 
         * $n_0% = numeric.
         * $s_0% = string.
         * $x_0% = expression (result from a function call).
         * $f_0% = document field placeholder.
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

        #region Public properties.

        public char? NextCharacter => _caret < _text.Length ? _text[_caret] : null;
        public bool IsEnd() => _caret >= _text.Length;
        public char[] TokenDelimiters => _standardTokenDelimiters;
        public int Caret => _caret;
        public int Length => _text.Length;
        public string Text => _text;

        /// <summary>
        /// The hash of the text.
        /// </summary>
        public string Hash
        {
            get
            {
                _hash ??= Library.Helpers.ComputeSHA256(_text);
                return _hash;
            }
        }

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
        public Tokenizer(string text, char[] standardTokenDelimiters, bool optimizeForTokenization = false, KbInsensitiveDictionary<string>? userParameters = null)
        {
            _text = new string(text.ToCharArray());
            _standardTokenDelimiters = standardTokenDelimiters;
            UserParameters = userParameters ?? new();

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
        public Tokenizer(string text, bool optimizeForTokenization = false, KbInsensitiveDictionary<string>? userParameters = null)
        {
            _text = new string(text.ToCharArray());
            _standardTokenDelimiters = ['\r', '\n', ' '];
            UserParameters = userParameters ?? new();

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

        #region Swap in/out literals.

        public KbInsensitiveDictionary<string> UserParameters { get; private set; } = new();
        public KbInsensitiveDictionary<QueryFieldLiteral> Literals { get; private set; } = new();

        /// <summary>
        /// Attempts to resolve a single string or numeric literal, otherwise returns the given value.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public string ResolveLiteral(string token)
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
            var regex = new Regex("\"([^\"\\\\]*(\\\\.[^\"\\\\]*)*)\"|\\'([^\\'\\\\]*(\\\\.[^\\'\\\\]*)*)\\'");

            while (true)
            {
                var match = regex.Match(query);

                if (match.Success)
                {
                    string key = $"$s_{_literalKey++}$";
                    Literals.Add(key, new(BasicDataType.String, match.ToString()[1..^1]));

                    query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Replaces numeric literals with tokens to prepare the query for parsing.
        /// </summary>
        private void SwapOutNumericLiterals(ref string query)
        {
            var regex = new Regex(@"(?<=\s|^)(?:\d+\.?\d*|\.\d+)(?=\s|$)");

            while (true)
            {
                var match = regex.Match(query);

                if (match.Success)
                {
                    string key = $"$n_{_literalKey++}$";
                    Literals.Add(key, new(BasicDataType.Numeric, match.ToString()));

                    query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Replaces numeric literals with tokens to prepare the query for parsing.
        /// </summary>
        private KbInsensitiveDictionary<string> InterpolateUserVariables(ref string query)
        {
            var mappings = new KbInsensitiveDictionary<string>();
            var regex = new Regex(@"(?<=\s|^)@\w+(?=\s|$)");  // Updated regex to match @ followed by alphanumeric characters

            while (true)
            {
                var match = regex.Match(query);

                if (match.Success)
                {
                    string key;

                    if (!UserParameters.TryGetValue(match.ToString(), out var userParameterValue))
                    {
                        throw new KbParserException($"Variable [{match}] is not defined.");
                    }

                    if (double.TryParse(match.ToString(), out _))
                    {
                        key = $"$n_{_literalKey++}$";
                        Literals.Add(key, new QueryFieldLiteral(BasicDataType.Numeric, userParameterValue));
                    }
                    else
                    {
                        key = $"$s_{_literalKey++}$";
                        Literals.Add(key, new QueryFieldLiteral(BasicDataType.String, userParameterValue[1..^1]));
                    }

                    mappings.Add(key, userParameterValue);

                    query = Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
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
            UserParameters = InterpolateUserVariables(ref text);

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
        /// Returns the position of the any of the given characters, seeks from the current caret position.
        /// </summary>
        public bool InertTryGetNextIndexOf(char[] characters, out int foundIndex)
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
        public int InertGetNextIndexOf(char[] characters)
        {
            if (InertTryGetNextIndexOf(characters, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Tokenizer character not found [{string.Join("],[", characters)}].");
        }

        /// <summary>
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public bool InertTryGetNextIndexOf(string[] givenStrings, out int foundIndex)
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
        public int InertGetNextIndexOf(string[] givenStrings)
        {
            if (InertTryGetNextIndexOf(givenStrings, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Expected string not found [{string.Join("],[", givenStrings)}].");
        }

        /// <summary>
        /// Returns the index of the given string. Throws exception if not found.
        /// </summary>
        public bool InertTryGetNextIndexOf(string givenString, out int foundIndex)
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
        public int InertGetNextIndexOf(string givenString)
        {
            if (InertTryGetNextIndexOf(givenString, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Expected string not found [{string.Join("],[", givenString)}].");
        }

        public delegate bool InertGetNextIndexOfProc(string peekedToken);

        /// <summary>
        /// Returns the index of the first found of the given strings. Throws exception if not found.
        /// </summary>
        public int InertGetNextIndexOf(InertGetNextIndexOfProc proc)
        {
            if (InertTryGetNextIndexOf(proc, out int foundIndex))
            {
                return foundIndex;
            }

            throw new Exception($"Expected string not found {proc.GetType().Name}.");
        }

        /// <summary>
        /// Returns true if the given strings is found.
        /// </summary>
        public bool InertTryGetNextIndexOf(InertGetNextIndexOfProc proc, out int foundIndex)
        {
            int restoreCaret = _caret;

            while (IsEnd() == false)
            {
                var token = GetNext();

                if (proc(token))
                {
                    foundIndex = _caret;
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
        public string SubString()
        {
            RecordBreadcrumb();

            var result = _text.Substring(_caret);
            _caret = _text.Length;

            InternalSkipWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position for a given length.
        /// </summary>
        public string SubString(int length)
        {
            RecordBreadcrumb();

            var result = _text.Substring(_caret, length);
            _caret += length;

            InternalSkipWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position to the given absolute position.
        /// </summary>
        public string SubStringAbsolute(int absoluteEndPosition)
        {
            RecordBreadcrumb();

            var result = _text.Substring(_caret, absoluteEndPosition - _caret);
            _caret = absoluteEndPosition;

            InternalSkipWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets the a substring from tokenizer from the internal caret position to the given absolute position.
        /// </summary>
        public string SubString(int startPosition, int length)
        {
            RecordBreadcrumb();

            var result = _text.Substring(startPosition, length);
            _caret = startPosition + length;

            InternalSkipWhiteSpace();
            return result;
        }

        /// <summary>
        /// Gets a substring from the tokenizer.
        /// </summary>
        public string InertSubString(int startPosition, int length)
            => _text.Substring(startPosition, length);

        /// <summary>
        /// Gets the remainder of the text from the given position.
        /// </summary>
        public string InertRemainderFrom(int startPosition)
            => _text.Substring(startPosition);

        /// <summary>
        /// Gets the remainder of the text from current caret position.
        /// </summary>
        public string InertRemainder()
            => _text.Substring(_caret);

        #endregion

        #region SkipNext.

        /// <summary>
        /// Skips the next token using the standard delimiters.
        /// </summary>
        public void SkipNext()
            => GetNext(_standardTokenDelimiters, out _);

        /// <summary>
        /// Skips the next token using the given delimiters.
        /// </summary>
        public void SkipNext(char[] delimiters)
            => GetNext(delimiters, out _);

        /// <summary>
        /// Skips the next token using the standard delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter..
        /// </summary>
        public void SkipNext(out char outStoppedOnDelimiter)
            => GetNext(_standardTokenDelimiters, out outStoppedOnDelimiter);

        /// <summary>
        /// Skips the next token using the given delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter..
        /// </summary>
        public void SkipNext(char[] delimiters, out char outStoppedOnDelimiter)
            => GetNext(delimiters, out outStoppedOnDelimiter);

        #endregion

        #region IsNextToken.

        public delegate bool TryNextTokenComparerProc(string peekedToken, string givenToken);
        public delegate bool TryNextTokenProc(string peekedToken);

        #region TryNextTokenEndsWith

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenEndsWith(string[] givenTokens, char[] delimiters)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenEndsWith(string[] givenTokens)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenEndsWith(string givenToken, char[] delimiters)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenEndsWith(string givenToken)
            => TryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters);

        #endregion

        #region TryNextTokenStartsWith

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenStartsWith(string[] givenTokens, char[] delimiters)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenStartsWith(string[] givenTokens)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenStartsWith(string givenToken, char[] delimiters)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenStartsWith(string givenToken)
            => TryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters);

        #endregion

        #region TryNextTokenContains

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenContains(string[] givenTokens, char[] delimiters)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenContains(string[] givenTokens)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// </summary>
        public bool TryNextTokenContains(string givenToken, char[] delimiters)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// </summary>
        public bool TryNextTokenContains(string givenToken)
            => TryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters);

        #endregion

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// </summary>
        public bool TryCompareNextToken(TryNextTokenComparerProc comparer, string[] givenTokens, char[] delimiters)
        {
            int restoreCaret = _caret;
            var token = GetNext(delimiters, out _);

            foreach (var givenToken in givenTokens)
            {
                if (comparer(token, givenToken))
                {
                    return true;
                }
            }

            _caret = restoreCaret;
            return false;
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// </summary>
        public bool TryIsNextToken(string[] givenTokens, char[] delimiters)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// </summary>
        public bool TryIsNextToken(string[] givenTokens)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// </summary>
        public bool TryIsNextToken(string givenToken)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// </summary>
        public bool TryIsNextToken(string givenToken, char[] delimiters)
            => TryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters);

        #endregion

        #region IsNextToken.

        public delegate bool InertTryNextTokenComparerProc(string peekedToken, string givenToken);

        #region InertTryNextTokenEndsWith

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// </summary>
        public bool InertTryNextTokenEndsWith(string[] givenTokens, char[] delimiters)
            => InertTryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// </summary>
        public bool InertTryNextTokenEndsWith(string[] givenTokens)
            => InertTryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the given delimiters.
        /// </summary>
        public bool InertTryNextTokenEndsWith(string givenToken, char[] delimiters)
            => InertTryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters);

        /// <summary>
        /// Returns true if the next token ends with any in the given array, using the standard delimiters.
        /// </summary>
        public bool InertTryNextTokenEndsWith(string givenToken)
            => InertTryCompareNextToken((p, g) => p.EndsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters);

        #endregion

        #region InertTryNextTokenStartsWith

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// </summary>
        public bool InertTryNextTokenStartsWith(string[] givenTokens, char[] delimiters)
            => InertTryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// </summary>
        public bool InertTryNextTokenStartsWith(string[] givenTokens)
            => InertTryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the given delimiters.
        /// </summary>
        public bool InertTryNextTokenStartsWith(string givenToken, char[] delimiters)
            => InertTryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters);

        /// <summary>
        /// Returns true if the next token begins with any in the given array, using the standard delimiters.
        /// </summary>
        public bool InertTryNextTokenStartsWith(string givenToken)
            => InertTryCompareNextToken((p, g) => p.StartsWith(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters);

        #endregion

        #region InertTryNextTokenContains

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// </summary>
        public bool InertTryNextTokenContains(string[] givenTokens, char[] delimiters)
            => InertTryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// </summary>
        public bool InertTryNextTokenContains(string[] givenTokens)
            => InertTryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the given delimiters.
        /// </summary>
        public bool InertTryNextTokenContains(string givenToken, char[] delimiters)
            => InertTryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters);

        /// <summary>
        /// Returns true if the next token contains any in the given array, using the standard delimiters.
        /// </summary>
        public bool InertTryNextTokenContains(string givenToken)
            => InertTryCompareNextToken((p, g) => p.Contains(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters);

        #endregion

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// </summary>
        public bool InertTryCompareNextToken(TryNextTokenProc comparer, char[] delimiters)
        {
            var token = InertGetNext(delimiters);
            return comparer(token);
        }


        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// </summary>
        public bool InertTryCompareNextToken(TryNextTokenProc comparer)
        {
            var token = InertGetNext();
            return comparer(token);
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// </summary>
        public bool InertTryCompareNextToken(TryNextTokenComparerProc comparer, string[] givenTokens, char[] delimiters)
        {
            var token = InertGetNext(delimiters);

            foreach (var givenToken in givenTokens)
            {
                if (comparer(token, givenToken))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the next token is in the given array, using the given delimiters.
        /// </summary>
        public bool InertTryIsNextToken(string[] givenTokens, char[] delimiters)
            => InertTryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, delimiters);

        /// <summary>
        /// Returns true if the next token is in the given array, using the standard delimiters.
        /// </summary>
        public bool InertTryIsNextToken(string[] givenTokens)
            => InertTryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), givenTokens, _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token matches the given token, using the standard delimiters.
        /// </summary>
        public bool InertTryIsNextToken(string givenToken)
            => InertTryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], _standardTokenDelimiters);

        /// <summary>
        /// Returns true if the next token matches the given token, using the given delimiters.
        /// </summary>
        public bool InertTryIsNextToken(string givenToken, char[] delimiters)
            => InertTryCompareNextToken((p, g) => p.Equals(g, StringComparison.InvariantCultureIgnoreCase), [givenToken], delimiters);

        #endregion

        #region GetNext.

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        public T GetNext<T>()
            => Helpers.Converters.ConvertTo<T>(GetNext(_standardTokenDelimiters, out _));

        /// <summary>
        /// Gets the next token using the standard delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter..
        /// </summary>
        public T GetNext<T>(out char outStoppedOnDelimiter)
            => Helpers.Converters.ConvertTo<T>(GetNext(_standardTokenDelimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters.
        /// </summary>
        public T GetNextEvaluated<T>(char[] delimiters)
            => Helpers.Converters.ConvertTo<T>(GetNextEvaluated(delimiters));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters.
        /// </summary>
        public T GetNextEvaluated<T>()
            => Helpers.Converters.ConvertTo<T>(GetNextEvaluated(_standardTokenDelimiters));

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        public T GetNext<T>(char[] delimiters)
            => Helpers.Converters.ConvertTo<T>(GetNext(delimiters, out _));

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        public string GetNext()
            => GetNext(_standardTokenDelimiters, out _);

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters.
        /// </summary>
        public string GetNextEvaluated()
            => ResolveLiteral(GetNext(_standardTokenDelimiters, out _));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters.
        /// </summary>
        public string GetNextEvaluated(char[] delimiters)
            => ResolveLiteral(GetNext(delimiters, out _));

        /// <summary>
        /// Gets the next token using the given delimiters,
        /// returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public T GetNext<T>(char[] delimiters, out char outStoppedOnDelimiter)
            => Helpers.Converters.ConvertTo<T>(GetNext(delimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token using the standard delimiters,
        /// returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string GetNext(out char outStoppedOnDelimiter)
            => GetNext(_standardTokenDelimiters, out outStoppedOnDelimiter);

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the standard delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string GetNextEvaluated(out char outStoppedOnDelimiter)
            => ResolveLiteral(GetNext(_standardTokenDelimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token, resolving it using the Numeric or String literals, using the given delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string GetNextEvaluated(char[] delimiters, out char outStoppedOnDelimiter)
            => ResolveLiteral(GetNext(delimiters, out outStoppedOnDelimiter));

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        public string GetNext(char[] delimiters)
            => GetNext(delimiters, out _);

        /// <summary>
        /// Gets the next token using the given delimiters, returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter.
        /// </summary>
        public string GetNext(char[] delimiters, out char outStoppedOnDelimiter)
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

            InternalSkipWhiteSpace();

            return token.Trim();
        }

        #endregion

        #region InertGetNext.

        /// <summary>
        /// Gets the next token using the standard delimiters.
        /// </summary>
        public T InertGetNext<T>()
            => Helpers.Converters.ConvertTo<T>(InertGetNext(_standardTokenDelimiters));

        /// <summary>
        /// Gets the next token using the given delimiters.
        /// </summary>
        public T InertGetNext<T>(char[] delimiters)
            => Helpers.Converters.ConvertTo<T>(InertGetNext(delimiters));

        /// <summary>
        /// Returns the next token without moving the caret.
        /// </summary>
        public string InertGetNext()
            => InertGetNext(_standardTokenDelimiters);

        /// <summary>
        /// Returns the next token without moving the caret using the given delimiters.
        /// </summary>
        public string InertGetNext(char[] delimiters)
        {
            int restoreCaret = _caret;
            var token = GetNext(delimiters, out _);
            _caret = restoreCaret;
            return token;
        }

        /// <summary>
        /// Returns the next token without moving the caret using the given delimiters,
        ///     returns the delimiter character that the tokenizer stopped on through outStoppedOnDelimiter..
        /// </summary>
        public string InertGetNext(char[] delimiters, out char outStoppedOnDelimiter)
        {
            int restoreCaret = _caret;
            var token = GetNext(delimiters, out outStoppedOnDelimiter);
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
            var next = NextCharacter ?? throw new KbParserException("The tokenizer sequence is empty.");
            return proc(next);
        }

        /// <summary>
        /// Returns true if the next character matches the given value.
        /// </summary>
        public bool InertIsNextCharacter(char character)
            => NextCharacter == character;

        public bool IsNextCharacter(char character)
        {
            RecordBreadcrumb();
            if (NextCharacter == character)
            {
                _caret++;
                InternalSkipWhiteSpace();
                return true;
            }
            return false;
        }

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
        /// Moves the caret forward while the character is in the given list, returns the count of skipped.
        /// </summary>
        public int SkipWhile(char[] characters)
        {
            int count = 0;
            while (_caret < _text.Length && characters.Contains(_text[_caret]))
            {
                count++;
                _caret++;
            }
            InternalSkipWhiteSpace();
            return count;
        }

        /// <summary>
        /// Moves the caret forward while the character matches the given value, returns the count of skipped.
        /// </summary>
        public int SkipWhile(char character)
            => SkipWhile([character]);

        /// <summary>
        /// Moves the caret forward by one character (then whitespace) if the character is in the given list, returns true if match was found.
        /// </summary>
        public bool SkipIf(char[] characters)
        {
            if (_caret < _text.Length && characters.Contains(_text[_caret]))
            {
                _caret++;
                InternalSkipWhiteSpace();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Moves the caret forward by one character (then whitespace) if the character matches the given value, returns true if match was found.
        /// </summary>
        public bool SkipIf(char character)
            => SkipIf([character]);

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
