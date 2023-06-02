using Katzebase.Engine.Indexes;
using Katzebase.Library.Exceptions;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition.NG
{
    public class NGCondition
    {
        public bool CoveredByIndex { get; set; } = false;
        public string SubsetKey { get; set; }
        public string ConditionKey { get; set; }
        public ConditionValue Left { get; set; } = new();
        public ConditionValue Right { get; set; } = new();
        public LogicalConnector LogicalConnector { get; set; } = LogicalConnector.None;
        public LogicalQualifier LogicalQualifier { get; set; } = LogicalQualifier.None;

        public NGCondition(string subsetKey, string conditionKey, LogicalConnector logicalConnector, string left, LogicalQualifier logicalQualifier, string right)
        {
            SubsetKey = subsetKey;
            ConditionKey = conditionKey;
            Left.Value = left;
            Right.Value = right;
            LogicalConnector = logicalConnector;
            LogicalQualifier = logicalQualifier;
        }

        public NGCondition Clone()
        {
            var clone = new NGCondition(SubsetKey, ConditionKey, LogicalConnector, Left.Value ?? string.Empty, LogicalQualifier, Right.Value ?? string.Empty);

            return clone;
        }

        public bool IsMatch(string passedValue)
        {
            if (LogicalQualifier == LogicalQualifier.Equals)
            {
                return passedValue == Right.Value;
            }
            else if (LogicalQualifier == LogicalQualifier.NotEquals)
            {
                return passedValue != Right.Value;
            }
            else if (LogicalQualifier == LogicalQualifier.GreaterThan)
            {
                if (decimal.TryParse(passedValue, out decimal left) && decimal.TryParse(Right.Value, out decimal right))
                {
                    return left > right;
                }
            }
            else if (LogicalQualifier == LogicalQualifier.LessThan)
            {
                if (decimal.TryParse(passedValue, out decimal left) && decimal.TryParse(Right.Value, out decimal right))
                {
                    return left < right;
                }
            }
            else if (LogicalQualifier == LogicalQualifier.GreaterThanOrEqual)
            {
                if (decimal.TryParse(passedValue, out decimal left) && decimal.TryParse(Right.Value, out decimal right))
                {
                    return left >= right;
                }
            }
            else if (LogicalQualifier == LogicalQualifier.LessThanOrEqual)
            {
                if (decimal.TryParse(passedValue, out decimal left) && decimal.TryParse(Right.Value, out decimal right))
                {
                    return left <= right;
                }
            }
            else if (LogicalQualifier == LogicalQualifier.Like)
            {
                var right = Right.Value;
                if (right != null)
                {
                    bool startsWith = right.StartsWith("%");
                    bool endsWith = right.EndsWith("%");

                    right = right.Trim('%');

                    if (startsWith == true && endsWith == true)
                    {
                        return passedValue.Contains(right);
                    }
                    else if (startsWith == true)
                    {
                        return passedValue.EndsWith(right);
                    }
                    else if (endsWith == true)
                    {
                        return passedValue.StartsWith(right);
                    }
                    else
                    {
                        return passedValue == Right.Value;
                    }
                }
            }
            else if (LogicalQualifier == LogicalQualifier.NotLike)
            {
                var right = Right.Value;
                if (right != null)
                {
                    bool startsWith = right.StartsWith("%");
                    bool endsWith = right.EndsWith("%");

                    right = right.Trim('%');

                    if (startsWith == true && endsWith == true)
                    {
                        return passedValue.Contains(right) == false;
                    }
                    else if (startsWith == true)
                    {
                        return passedValue.EndsWith(right) == false;
                    }
                    else if (endsWith == true)
                    {
                        return passedValue.StartsWith(right) == false;
                    }
                    else
                    {
                        return passedValue == Right.Value == false;
                    }
                }
            }
            else
            {
                throw new KbParserException("Unsupprted condition type.");
            }

            return false;
        }
    }
}
