using NTDLS.Katzebase.Client.Exceptions;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Query.Tokenizers
{
    /// <summary>
    /// Used for parsing WHERE clauses and join expressions.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public class ConditionTokenizer
    {
        static readonly char[] DefaultTokenDelimiters = [','];

        private string _text;
        private int _position = 0;
        private readonly int _startPosition = 0;

        public string Text => _text;
        public int Position => _position;
        public int Length => _text.Length;
        public int StartPosition => _startPosition;

        public ConditionTokenizer(string text)
        {
            _text = text;
        }

        public ConditionTokenizer(string text, int startPosition)
        {
            _text = text;
            _position = startPosition;
            _startPosition = startPosition;
        }

        static public LogicalQualifier ParseLogicalQualifier(string text)
        {
            return text.ToLowerInvariant() switch
            {
                "=" => LogicalQualifier.Equals,
                "!=" => LogicalQualifier.NotEquals,
                ">" => LogicalQualifier.GreaterThan,
                "<" => LogicalQualifier.LessThan,
                ">=" => LogicalQualifier.GreaterThanOrEqual,
                "<=" => LogicalQualifier.LessThanOrEqual,
                "like" => LogicalQualifier.Like,
                "not like" => LogicalQualifier.NotLike,
                "between" => LogicalQualifier.Between,
                "not between" => LogicalQualifier.NotBetween,
                _ => LogicalQualifier.None,
            };
        }

        public static string LogicalQualifierToString(LogicalQualifier logicalQualifier)
        {
            return logicalQualifier switch
            {
                LogicalQualifier.Equals => "=",
                LogicalQualifier.NotEquals => "!=",
                LogicalQualifier.GreaterThanOrEqual => ">=",
                LogicalQualifier.LessThanOrEqual => "<=",
                LogicalQualifier.LessThan => "<",
                LogicalQualifier.GreaterThan => ">",
                LogicalQualifier.Like => "~",
                LogicalQualifier.NotLike => "!~",
                _ => "",
            };
        }

        public static string LogicalConnectorToString(LogicalConnector logicalConnector)
        {
            return logicalConnector == LogicalConnector.None ? string.Empty : logicalConnector.ToString().ToUpper();
        }

        public static string LogicalConnectorToOperator(LogicalConnector logicalConnector)
        {
            switch (logicalConnector)
            {
                case LogicalConnector.Or:
                    return "||";
                case LogicalConnector.And:
                    return "&&";
            }

            return string.Empty;
        }

        public void SetText(string text, int position)
        {
            _text = text;
            _position = position;
            if (_position >= _text.Length)
            {
                throw new KbParserException("Skip position is greater than text length.");
            }
        }

        public void SetText(string text)
        {
            _text = text;
            if (_position >= _text.Length)
            {
                throw new KbParserException("Skip position is greater than text length.");
            }
        }

        public void SetPosition(int position)
        {
            _position = position;
            if (_position > _text.Length)
            {
                throw new KbParserException("Skip position is greater than text length.");
            }
        }

        public void SkipDelimiters()
        {
            SkipDelimiters(DefaultTokenDelimiters);
        }

        public static void SkipDelimiters(string text, ref int position)
        {
            SkipDelimiters(text, ref position, DefaultTokenDelimiters);
        }

        public void SkipDelimiters(char[] delimiters)
        {
            SkipDelimiters(_text, ref _position, delimiters);
        }

        public static void SkipDelimiters(string text, ref int position, char[] delimiters)
        {
            while (position < text.Length && (char.IsWhiteSpace(text[position]) || delimiters.Contains(text[position]) == true))
            {
                position++;
            }
        }

        public void SkipWhiteSpace()
        {
            SkipWhiteSpace(_text, ref _position);
        }

        public static void SkipWhiteSpace(string text, ref int position)
        {
            while (position < text.Length && char.IsWhiteSpace(text[position]))
            {
                position++;
            }
        }

        public string PeekNext()
        {
            int originalPosition = _position;
            var result = GetNext();
            _position = originalPosition;
            return result;
        }

        public void SkipNext()
        {
            _ = GetNext();
        }

        public string GetNext()
        {
            return GetNext(_text, ref _position);
        }

        public static string GetNext(string text, ref int position)
        {
            var token = string.Empty;

            if (position == text.Length)
            {
                return string.Empty;
            }

            if (new char[] { '(', ')' }.Contains(text[position]))
            {
                token += text[position];
                position++;
                SkipWhiteSpace(text, ref position);
                return token;
            }

            for (; position < text.Length; position++)
            {
                if (char.IsWhiteSpace(text[position]) || new char[] { '(', ')' }.Contains(text[position]))
                {
                    break;
                }

                token += text[position];
            }

            SkipWhiteSpace(text, ref position);
            SkipDelimiters(text, ref position);

            return token.Trim().ToLowerInvariant();
        }
    }
}
