using Katzebase.PublicLibrary.Exceptions;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Query.Tokenizers
{
    public class ConditionTokenizer
    {
        static char[] DefaultTokenDelimiters = new char[] { ',' };

        private string _text;
        private int _position = 0;
        private int _startPosition = 0;

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
            switch (text)
            {
                case "=":
                    return LogicalQualifier.Equals;
                case "!=":
                    return LogicalQualifier.NotEquals;
                case ">":
                    return LogicalQualifier.GreaterThan;
                case "<":
                    return LogicalQualifier.LessThan;
                case ">=":
                    return LogicalQualifier.GreaterThanOrEqual;
                case "<=":
                    return LogicalQualifier.LessThanOrEqual;
                case "~":
                    return LogicalQualifier.Like;
                case "!~":
                    return LogicalQualifier.NotLike;
            }
            return LogicalQualifier.None;
        }

        public static string LogicalQualifierToString(LogicalQualifier logicalQualifier)
        {
            switch (logicalQualifier)
            {
                case LogicalQualifier.Equals:
                    return "=";
                case LogicalQualifier.NotEquals:
                    return "!=";
                case LogicalQualifier.GreaterThanOrEqual:
                    return ">=";
                case LogicalQualifier.LessThanOrEqual:
                    return "<=";
                case LogicalQualifier.LessThan:
                    return "<";
                case LogicalQualifier.GreaterThan:
                    return ">";
            }

            return "";
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
                throw new KbParserException("Skip position is greater than query length.");
            }
        }

        public void SetText(string text)
        {
            _text = text;
            if (_position >= _text.Length)
            {
                throw new KbParserException("Skip position is greater than query length.");
            }
        }

        public void SetPosition(int position)
        {
            _position = position;
            if (_position >= _text.Length)
            {
                throw new KbParserException("Skip position is greater than query length.");
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

        public string PeekNextToken()
        {
            int originalPosition = _position;
            var result = GetNextToken();
            _position = originalPosition;
            return result;
        }

        public void SkipNextToken()
        {
            _ = GetNextToken();
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

        public bool IsNextToken(string token)
        {
            return PeekNextToken().ToLower() == token.ToLower();
        }

        /// <summary>
        /// Used for parsing WHERE clauses.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public string GetNextToken()
        {
            return GetNextToken(_text, ref _position);
        }

        public static string GetNextToken(string text, ref int position)
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
