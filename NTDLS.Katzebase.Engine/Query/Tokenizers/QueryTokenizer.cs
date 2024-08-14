using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using System.Text.RegularExpressions;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Query.Tokenizers
{
    public class QueryTokenizer
    {
        static readonly char[] DefaultTokenDelimiters = [',', '='];

        private readonly string _text;
        private int _position = 0;
        private readonly int _startPosition = 0;

        public string Text => _text;
        public int Position => _position;
        public int Length => _text.Length;
        public int StartPosition => _startPosition;
        public KbInsensitiveDictionary<string> StringLiterals { get => _stringLiterals; }
        public KbInsensitiveDictionary<string> NumericLiterals { get => _numericLiterals; }
        public List<string> Breadcrumbs { get; private set; } = new();
        public char? NextCharacter => _position < _text.Length ? _text[_position] : null;
        public bool IsEnd() => _position >= _text.Length;

        /// <summary>
        /// After the constructor is called, this will contain the same hash
        ///     for the same query regardless of string or numeric constants.
        /// </summary>
        public string LogicHash { get; private set; }

        private readonly KbInsensitiveDictionary<string> _stringLiterals;
        private readonly KbInsensitiveDictionary<string> _numericLiterals;

        public QueryTokenizer(string text)
        {
            _text = text.Trim().TrimEnd(';').Trim();
            CleanQueryText(ref _text, out _stringLiterals, out _numericLiterals);

            LogicHash = Library.Helpers.GetSHA256Hash(_text);
        }

        public QueryTokenizer(string text, int startPosition)
        {
            _text = text;
            _position = startPosition;
            _startPosition = startPosition;
            CleanQueryText(ref _text, out _stringLiterals, out _numericLiterals);

            LogicHash = Library.Helpers.GetSHA256Hash(_text);
        }

        public void SwapFieldLiteral(ref string givenValue)
        {
            if (string.IsNullOrEmpty(givenValue) == false && StringLiterals.TryGetValue(givenValue, out string? value))
            {
                givenValue = value;

                if (givenValue.StartsWith('\'') && givenValue.EndsWith('\''))
                {
                    givenValue = givenValue.Substring(1, givenValue.Length - 2);
                }
            }
        }

        public void SetPosition(int position)
        {
            _position = position;
            if (_position > _text.Length)
            {
                throw new KbParserException("Skip position is greater than query length.");
            }
        }

        public char CurrentChar()
        {
            if (_position >= Length)
            {
                return '\0';
            }
            return (_text.Substring(_position, 1)[0]);
        }

        public bool IsNextCharacter(char ch)
        {
            if (_position >= Length)
            {
                return false;
            }
            return (_text.Substring(_position, 1)[0] == ch);
        }

        public string Remainder()
        {
            return _text.Substring(_position).Trim();
        }

        public string GetNext(char[] delimiters)
        {
            var token = string.Empty;

            if (_position == _text.Length)
            {
                Breadcrumbs.Add(string.Empty);
                return string.Empty;
            }

            for (; _position < _text.Length; _position++)
            {
                if (char.IsWhiteSpace(_text[_position]) || delimiters.Contains(_text[_position]) == true)
                {
                    break;
                }

                token += _text[_position];
            }

            SkipWhiteSpace();

            token = token.Trim();

            Breadcrumbs.Add(token);
            return token;
        }

        public string GetNext()
        {
            return GetNext(DefaultTokenDelimiters);
        }

        public int GetNextAsInt()
        {
            string token = GetNext();
            if (int.TryParse(token, out int value) == false)
            {
                throw new KbParserException("Invalid query. Found [" + token + "], expected numeric row limit.");
            }

            return value;
        }

        public bool IsNextStartOfQuery()
        {
            return IsNextStartOfQuery(out var _);
        }

        public bool IsNextStartOfQuery(out QueryType type)
        {
            var token = PeekNext().ToLowerInvariant();

            return Enum.TryParse(token, true, out type) //Enum parse.
                && Enum.IsDefined(typeof(QueryType), type) //Is enum value über lenient.
                && int.TryParse(token, out _) == false; //Is not number, because enum parsing is "too" flexible.
        }

        public string PeekNext()
        {
            int originalPosition = _position;
            var result = GetNext(DefaultTokenDelimiters);
            _position = originalPosition;
            return result;
        }

        public string PeekNext(char[] delimiters)
        {
            int originalPosition = _position;
            var result = GetNext(delimiters);
            _position = originalPosition;
            return result;
        }

        public void SkipNext()
        {
            GetNext(DefaultTokenDelimiters);
        }

        public void SkipNext(char[] delimiters)
        {
            GetNext(delimiters);
        }

        public void SkipToEnd()
        {
            _position = _text.Length;
        }

        public void SkipDelimiters()
        {
            SkipDelimiters(DefaultTokenDelimiters);
        }

        public void SkipDelimiters(char delimiter)
        {
            SkipDelimiters([delimiter]);
        }

        public void SkipDelimiters(char[] delimiters)
        {
            while (_position < _text.Length && (char.IsWhiteSpace(_text[_position]) || delimiters.Contains(_text[_position]) == true))
            {
                _position++;
            }
        }

        public void SkipWhile(char[] chs)
        {
            while (_position < _text.Length && (chs.Contains(_text[_position]) || char.IsWhiteSpace(_text[_position])))
            {
                _position++;
            }
        }

        public void SkipWhile(char ch)
        {
            while (_position < _text.Length && (_text[_position] == ch || char.IsWhiteSpace(_text[_position])))
            {
                _position++;
            }
        }

        public void SkipNextCharacter()
        {
            _position++;

            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }

        public void SkipWhiteSpace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }

        /// <summary>
        /// Removes all unnecessary whitespace, newlines, comments and replaces literals with tokens to prepare query for parsing.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="swapLiteralsBackIn"></param>
        /// <returns></returns>
        public static void CleanQueryText(ref string query,
            out KbInsensitiveDictionary<string> stringLiterals,
            out KbInsensitiveDictionary<string> numericLiterals)
        {
            query = KbTextUtility.RemoveComments(query);

            stringLiterals = SwapOutLiteralStrings(ref query);

            //We replace numeric constants and we want to make sure we have 
            //  no numbers next to any conditional operators before we do so.
            query = query.Replace(">", " > ");
            query = query.Replace(">", " < ");
            query = query.Replace("=", " = ");
            query = query.Replace("!=", " != ");
            query = query.Replace("> =", ">=");
            query = query.Replace("< =", "<=");
            query = query.Replace(">=", " >= ");
            query = query.Replace("<=", " <= ");
            query = query.Replace("||", " || ");
            query = query.Replace("&&", " && ");

            numericLiterals = SwapOutLiteralNumeric(ref query);

            int length;
            do
            {
                length = query.Length;
                query = query.Replace("\t", " ");
                query = query.Replace("  ", " ");
            }
            while (length != query.Length);

            query = query.Trim();

            query = query.Replace("(", " ( ").Replace(")", " ) ");

            RemoveComments(ref query);

            TrimAllLines(ref query);
            RemoveEmptyLines(ref query);
            RemoveNewlines(ref query);
            RemoveDoubleWhitespace(ref query);

            query = query.Trim();
        }

        /// <summary>
        /// Replaces literals with tokens to prepare query for parsing.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static KbInsensitiveDictionary<string> SwapOutLiteralStrings(ref string query)
        {
            //TODO: Replace with NTDLS.Helpers.Text when nugets get updated.
            static string ReplaceRange(string original, int startIndex, int length, string replacement)
            {
                // Remove the range of text to be replaced
                string removed = original.Remove(startIndex, length);
                // Insert the replacement string at the start index
                string result = removed.Insert(startIndex, replacement);
                return result;
            }

            var mappings = new KbInsensitiveDictionary<string>();

            var regex = new Regex("\"([^\"\\\\]*(\\\\.[^\"\\\\]*)*)\"|\\'([^\\'\\\\]*(\\\\.[^\\'\\\\]*)*)\\'");

            int literalKey = 0;

            while (true)
            {
                var match = regex.Match(query);

                if (match.Success)
                {
                    string key = $"$S_{literalKey++}$";
                    mappings.Add(key, match.ToString());

                    query = ReplaceRange(query, match.Index, match.Length, key);

                    //query = query.Replace(match.ToString(), key);
                }
                else
                {
                    break;
                }
            }

            return mappings;
        }

        public static KbInsensitiveDictionary<string> SwapOutLiteralNumeric(ref string query)
        {
            //TODO: Replace with NTDLS.Helpers.Text when nugets get updated.
            static string ReplaceRange(string original, int startIndex, int length, string replacement)
            {
                // Remove the range of text to be replaced
                string removed = original.Remove(startIndex, length);
                // Insert the replacement string at the start index
                string result = removed.Insert(startIndex, replacement);
                return result;
            }

            var mappings = new KbInsensitiveDictionary<string>();

            int literalKey = 0;

            var regex = new Regex(@"(?<=\s|^)(?:\d+\.?\d*|\.\d+)(?=\s|$)");

            while (true)
            {
                var match = regex.Match(query);

                if (match.Success)
                {
                    string key = $"%N_{literalKey++}%";
                    mappings.Add(key, match.ToString());

                    query = ReplaceRange(query, match.Index, match.Length, key);

                    //query = query.Replace(match.ToString(), key);
                }
                else
                {
                    break;
                }
            }

            return mappings;
        }

        public static void RemoveDoubleWhitespace(ref string query)
        {
            query = Regex.Replace(query, @"\s+", " ");
        }

        public static void RemoveNewlines(ref string query)
        {
            query = query.Replace("\r\n", " ");
        }

        public static void SwapInLiteralStrings(ref string query, KbInsensitiveDictionary<string> mappings)
        {
            foreach (var mapping in mappings)
            {
                query = query.Replace(mapping.Key, mapping.Value);
            }
        }

        public static void RemoveComments(ref string query)
        {
            query = "\r\n" + query + "\r\n";

            var blockComments = @"/\*(.*?)\*/";
            //var lineComments = @"//(.*?)\r?\n";
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
        {
            query = Regex.Replace(query, @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
        }

        public static void TrimAllLines(ref string query)
        {
            query = string.Join("\r\n", query.Split('\n').Select(o => o.Trim()));
        }
    }
}
