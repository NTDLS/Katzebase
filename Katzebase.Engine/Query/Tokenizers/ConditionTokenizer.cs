using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Query.Tokenizers
{
    public static class ConditionTokenizer
    {
        static char[] DefaultTokenDelimiters = new char[] { ',' };

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

        public static void SkipDelimiters(string query, ref int position)
        {
            SkipDelimiters(query, DefaultTokenDelimiters, ref position);
        }

        public static void SkipWhiteSpace(string query, ref int position)
        {
            while (position < query.Length && char.IsWhiteSpace(query[position]))
            {
                position++;
            }
        }

        public static void SkipDelimiters(string query, char[] delimiters, ref int position)
        {
            while (position < query.Length && (char.IsWhiteSpace(query[position]) || delimiters.Contains(query[position]) == true))
            {
                position++;
            }
        }

        /// <summary>
        /// Used for parsing WHERE clauses.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static string GetNextClauseToken(string query, ref int position)
        {
            var token = string.Empty;

            if (position == query.Length)
            {
                return string.Empty;
            }

            if (new char[] { '(', ')' }.Contains(query[position]))
            {
                token += query[position];
                position++;
                SkipWhiteSpace(query, ref position);
                return token;
            }

            for (; position < query.Length; position++)
            {
                if (char.IsWhiteSpace(query[position]) || new char[] { '(', ')' }.Contains(query[position]))
                {
                    break;
                }

                token += query[position];
            }

            SkipWhiteSpace(query, ref position);
            SkipDelimiters(query, ref position);

            return token.Trim().ToLowerInvariant();
        }
    }
}
