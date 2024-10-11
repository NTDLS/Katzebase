using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Conditions
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

        private static readonly string[] _logicalQualifiers = ["=", "!=", ">", "<", ">=", "<=", "like", "not like", "between", "not between"];
        public static bool IsLogicalQualifier(string text)
           => _logicalQualifiers.Contains(text.ToLowerInvariant());

        private static readonly string[] _logicalConnectors = ["and", "or"];
        public static bool IsLogicalConnector(string text)
           => _logicalConnectors.Contains(text.ToLowerInvariant());
    }
}
