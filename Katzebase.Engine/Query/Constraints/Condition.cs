using Katzebase.PublicLibrary.Exceptions;
using static Katzebase.Engine.KbLib.EngineConstants;

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

        public bool IsMatch(string passedValue)
        {
            return IsMatch(passedValue, this.LogicalQualifier, Right.Value);
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
                if (decimal.TryParse(leftString, out decimal left) && decimal.TryParse(rightString, out decimal right))
                {
                    return left > right;
                }
            }
            else if (logicalQualifier == LogicalQualifier.LessThan)
            {
                if (decimal.TryParse(leftString, out decimal left) && decimal.TryParse(rightString, out decimal right))
                {
                    return left < right;
                }
            }
            else if (logicalQualifier == LogicalQualifier.GreaterThanOrEqual)
            {
                if (decimal.TryParse(leftString, out decimal left) && decimal.TryParse(rightString, out decimal right))
                {
                    return left >= right;
                }
            }
            else if (logicalQualifier == LogicalQualifier.LessThanOrEqual)
            {
                if (decimal.TryParse(leftString, out decimal left) && decimal.TryParse(rightString, out decimal right))
                {
                    return left <= right;
                }
            }
            else if (logicalQualifier == LogicalQualifier.Like
                || logicalQualifier == LogicalQualifier.NotLike)
            {
                var right = rightString;
                bool result = false;

                if (right != null)
                {
                    bool startsWith = right.StartsWith("%");
                    bool endsWith = right.EndsWith("%");


                    right = right.Trim('%');

                    if (startsWith == true && endsWith == true)
                    {
                        result = leftString?.Contains(right) ?? false;
                    }
                    else if (startsWith == true)
                    {
                        result = leftString?.EndsWith(right) ?? false;
                    }
                    else if (endsWith == true)
                    {
                        result = leftString?.StartsWith(right) ?? false;
                    }
                    else
                    {
                        result = leftString == rightString;
                    }
                }

                if (logicalQualifier == LogicalQualifier.NotLike)
                {
                    return result == false;
                }
                return result;
            }
            else
            {
                throw new KbParserException("Unsupprted condition type.");
            }

            return false;
        }
    }
}