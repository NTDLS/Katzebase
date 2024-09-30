using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions.Helpers
{
    public static class StaticConditionHelpers
    {
        public static LogicalQualifier ParseLogicalQualifier(Tokenizer tokenizer, string text)
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
                _ => throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected logical qualifier, found: [{text}]."),
            };
        }

        public static bool IsLogicalQualifier(string text)
           => (new string[] { "=", "!=", ">", "<", ">=", "<=", "like", "not like", "between", "not between" }).Contains(text.ToLowerInvariant());

        public static string LogicalQualifierToString(Tokenizer tokenizer, LogicalQualifier logicalQualifier)
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
                _ => throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected logical qualifier, found: [{logicalQualifier}].")
            };
        }

        public static bool IsLogicalConnector(string text)
           => (new string[] { "and", "or" }).Contains(text.ToLowerInvariant());

        public static string LogicalConnectorToString(LogicalConnector logicalConnector)
            => logicalConnector == LogicalConnector.None ? string.Empty : logicalConnector.ToString().ToUpper();

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
    }
}
