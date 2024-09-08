using NTDLS.Katzebase.Client.Exceptions;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
{
    internal static class StaticConditionHelpers
    {
        public static LogicalQualifier ParseLogicalQualifier(string text)
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
                _ => throw new KbParserException($"Unexpected logical qualifier found: [{text}]."),
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
                _ => throw new KbParserException($"Unexpected logical qualifier found [{logicalQualifier}].")
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

    }
}
