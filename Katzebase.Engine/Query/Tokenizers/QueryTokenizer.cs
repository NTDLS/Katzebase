using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using System.Text.RegularExpressions;

namespace Katzebase.Engine.Query.Tokenizers
{
    public class QueryTokenizer
    {
        static readonly char[] DefaultTokenDelimiters = new char[] { ',', '=' };

        private readonly string _text;
        private int _position = 0;
        private readonly int _startPosition = 0;

        public string Text => _text;
        public int Position => _position;
        public int Length => _text.Length;
        public int StartPosition => _startPosition;
        public Dictionary<string, string> LiteralStrings { get; private set; }
        public List<string> Breadcrumbs { get; private set; } = new();
        public char? NextCharacter => _position < _text.Length ? _text[_position] : null;
        public bool IsEnd() => _position == _text.Length;

        public QueryTokenizer(string text)
        {
            _text = text.Trim().TrimEnd(';').Trim();
            LiteralStrings = CleanQueryText(ref _text);
        }

        public QueryTokenizer(string text, int startPosition)
        {
            _text = text;
            _position = startPosition;
            _startPosition = startPosition;
            LiteralStrings = CleanQueryText(ref _text);
        }

        public void SwapFieldLiteral(ref string value)
        {
            if (string.IsNullOrEmpty(value) == false && LiteralStrings.ContainsKey(value))
            {
                value = LiteralStrings[value];

                if (value.StartsWith('\'') && value.EndsWith('\''))
                {
                    value = value.Substring(1, value.Length - 2);
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
            return (_text.Substring(_position, 1)[0]);
        }

        public bool IsNextCharacter(char ch)
        {
            return (_text.Substring(_position, 1)[0] == ch);
        }

        public string Remainder()
        {
            return _text.Substring(_position).Trim();
        }

        public int GetNextTokenAsInt()
        {
            string token = GetNextToken();
            if (int.TryParse(token, out int value) == false)
            {
                throw new KbParserException("Invalid query. Found [" + token + "], expected numeric row limit.");
            }

            return value;
        }

        public string GetNextToken()
        {
            return GetNextToken(DefaultTokenDelimiters);
        }

        public bool IsNextTokenConsume(string[] tokens)
        {
            var token = GetNextToken().ToLower();
            foreach (var given in tokens)
            {
                if (token == given.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsNextToken(string[] tokens)
        {
            var token = PeekNextToken().ToLower();
            foreach (var given in tokens)
            {
                if (token == given.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsNextTokenConsume(string token)
        {
            return GetNextToken().ToLower() == token.ToLower();
        }

        public bool IsNextToken(string token)
        {
            return PeekNextToken().ToLower() == token.ToLower();
        }

        public string PeekNextToken()
        {
            int originalPosition = _position;
            var result = GetNextToken(DefaultTokenDelimiters);
            _position = originalPosition;
            return result;
        }

        public void SkipNextToken()
        {
            GetNextToken(DefaultTokenDelimiters);
        }

        public void SkipToEnd()
        {
            _position = _text.Length;
        }

        public string PeekNextToken(char[] delimiters)
        {
            int originalPosition = _position;
            var result = GetNextToken(delimiters);
            _position = originalPosition;
            return result;
        }

        public void SkipNextToken(char[] delimiters)
        {
            GetNextToken(delimiters);
        }

        public string GetNextToken(char[] delimiters)
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

        public void SkipDelimiters()
        {
            SkipDelimiters(DefaultTokenDelimiters);
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

        public void SkipDelimiters(char delimiter)
        {
            SkipDelimiters(new char[] { delimiter });
        }

        public void SkipDelimiters(char[] delimiters)
        {
            while (_position < _text.Length && (char.IsWhiteSpace(_text[_position]) || delimiters.Contains(_text[_position]) == true))
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
        public static Dictionary<string, string> CleanQueryText(ref string query, bool swapLiteralsBackIn = false)
        {
            query = KbUtility.RemoveComments(query);

            var literalStrings = SwapOutLiteralStrings(ref query);
            query = query.Trim();

            query = query.Replace("(", " ( ").Replace(")", " ) ");

            RemoveComments(ref query);
            if (swapLiteralsBackIn)
            {
                SwapInLiteralStrings(ref query, literalStrings);
            }
            TrimAllLines(ref query);
            RemoveEmptyLines(ref query);
            RemoveNewlines(ref query);
            RemoveDoubleWhitespace(ref query);
            query = query.Trim();
            return literalStrings;
        }

        /// <summary>
        /// Replaces literals with tokens to prepare query for parsing.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static Dictionary<string, string> SwapOutLiteralStrings(ref string query)
        {
            var mappings = new Dictionary<string, string>();

            var regex = new Regex("\"([^\"\\\\]*(\\\\.[^\"\\\\]*)*)\"|\\'([^\\'\\\\]*(\\\\.[^\\'\\\\]*)*)\\'");
            var results = regex.Matches(query);

            foreach (Match match in results)
            {
                string uuid = $"${Guid.NewGuid()}$";

                mappings.Add(uuid, match.ToString());

                query = query.Replace(match.ToString(), uuid);
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

        public static void SwapInLiteralStrings(ref string query, Dictionary<string, string> mappings)
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
