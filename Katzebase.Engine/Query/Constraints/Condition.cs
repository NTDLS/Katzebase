using Katzebase.PublicLibrary.Exceptions;
using System.Text.RegularExpressions;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Query.Constraints
{
    internal class Condition
    {
        public bool CoveredByIndex { get; set; } = false;
        public string SubsetKey { get; set; }
        public string ConditionKey { get; set; }
        public SmartValue Left { get; set; } = new();
        public SmartValue Right { get; set; } = new();
        public LogicalConnector LogicalConnector { get; set; } = LogicalConnector.None;
        public LogicalQualifier LogicalQualifier { get; set; } = LogicalQualifier.None;

        public Condition(string subsetKey, string conditionKey, LogicalConnector logicalConnector, string left, LogicalQualifier logicalQualifier, string right)
        {
            SubsetKey = subsetKey;
            ConditionKey = conditionKey;
            Left.Value = left;
            Right.Value = right;
            LogicalConnector = logicalConnector;
            LogicalQualifier = logicalQualifier;
        }

        public Condition(string subsetKey, string conditionKey, LogicalConnector logicalConnector, LogicalQualifier logicalQualifier)
        {
            SubsetKey = subsetKey;
            ConditionKey = conditionKey;
            LogicalConnector = logicalConnector;
            LogicalQualifier = logicalQualifier;
        }

        public Condition Clone()
        {
            var clone = new Condition(SubsetKey, ConditionKey, LogicalConnector, LogicalQualifier)
            {
                Left = Left.Clone(),
                Right = Right.Clone()
            };

            return clone;
        }

        public bool IsMatch(string? passedValue)
        {
            return IsMatch(passedValue, LogicalQualifier, Right.Value);
        }

        public static bool? IsMatchGreaterOrEqualAsDecimal(string? left, string? right)
        {
            if (left != null && right != null && int.TryParse(left, out var iLeft))
            {
                if (decimal.TryParse(right, out var iRight))
                {
                    return iLeft >= iRight;
                }

            }
            return null;
        }

        public static bool? IsMatchLesserOrEqualAsDecimal(string? left, string? right)
        {
            if (left != null && right != null && int.TryParse(left, out var iLeft))
            {
                if (decimal.TryParse(right, out var iRight))
                {
                    return iLeft <= iRight;
                }

            }
            return null;
        }

        public static bool? IsMatchGreaterAsDecimal(string? left, string? right)
        {
            if (left != null && right != null && int.TryParse(left, out var iLeft))
            {
                if (decimal.TryParse(right, out var iRight))
                {
                    return iLeft > iRight;
                }

            }
            return null;
        }
        public static bool? IsMatchLesserAsDecimal(string? left, string? right)
        {
            if (left != null && right != null && int.TryParse(left, out var iLeft))
            {
                if (decimal.TryParse(right, out var iRight))
                {
                    return iLeft < iRight;
                }

            }
            return null;
        }

        public static bool? IsMatchLike(string? input, string? pattern)
        {
            if (input == null || pattern == null)
            {
                return null;
            }

            string regexPattern = "^" + Regex.Escape(pattern).Replace("%", ".*").Replace("_", ".") + "$";
            return Regex.IsMatch(input, regexPattern);
        }

        public static bool IsMatch(string? leftString, LogicalQualifier logicalQualifier, string? rightString)
        {
            if (logicalQualifier == LogicalQualifier.Equals)
            {
                return leftString == rightString;
            }
            else if (logicalQualifier == LogicalQualifier.NotEquals)
            {
                return leftString != rightString;
            }
            else if (logicalQualifier == LogicalQualifier.GreaterThan)
            {
                return IsMatchGreaterAsDecimal(leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.LessThan)
            {
                return IsMatchLesserAsDecimal(leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.GreaterThanOrEqual)
            {
                return IsMatchGreaterOrEqualAsDecimal(leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.LessThanOrEqual)
            {
                return IsMatchLesserOrEqualAsDecimal(leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.Like)
            {
                return IsMatchLike(leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.NotLike)
            {
                return IsMatchLike(leftString, rightString) == false;
            }
            else
            {
                throw new KbParserException("Unsupprted condition type.");
            }
        }
    }
}