using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class ConditionSingle : ConditionBase
    {
        public ConditionValue Left { get; set; } = new ConditionValue();
        public ConditionValue Right { get; set; } = new ConditionValue();


        public LogicalQualifier LogicalQualifier { get; set; } = LogicalQualifier.None;
        public LogicalConnector LogicalConnector { get; set; } = LogicalConnector.None;

        public ConditionSingle(LogicalConnector logicalConnector, string left)
        {
            LogicalConnector = logicalConnector;
            Left.Value = left;
        }

        public ConditionSingle(LogicalConnector logicalConnector)
        {
            LogicalConnector = logicalConnector;
        }

        public ConditionSingle()
        {
        }

        public bool IsMatch(string passedValue)
        {
            if (this.LogicalQualifier == LogicalQualifier.Equals)
            {
                return (passedValue == this.Right.Value);
            }
            else if (this.LogicalQualifier == LogicalQualifier.NotEquals)
            {
                return (passedValue != this.Right.Value);
            }
            else if (this.LogicalQualifier == LogicalQualifier.GreaterThan)
            {
                if (Decimal.TryParse(passedValue, out decimal left) && Decimal.TryParse(this.Right.Value, out decimal right))
                {
                    return (left > right);
                }
            }
            else if (this.LogicalQualifier == LogicalQualifier.LessThan)
            {
                if (Decimal.TryParse(passedValue, out decimal left) && Decimal.TryParse(this.Right.Value, out decimal right))
                {
                    return (left < right);
                }
            }
            else if (this.LogicalQualifier == LogicalQualifier.GreaterThanOrEqual)
            {
                if (Decimal.TryParse(passedValue, out decimal left) && Decimal.TryParse(this.Right.Value, out decimal right))
                {
                    return (left >= right);
                }
            }
            else if (this.LogicalQualifier == LogicalQualifier.LessThanOrEqual)
            {
                if (Decimal.TryParse(passedValue, out decimal left) && Decimal.TryParse(this.Right.Value, out decimal right))
                {
                    return (left <= right);
                }
            }
            else if (this.LogicalQualifier == LogicalQualifier.Like)
            {
                var right = this.Right.Value;
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
                        return (passedValue == this.Right.Value);
                    }
                }
            }
            else if (this.LogicalQualifier == LogicalQualifier.NotLike)
            {
                var right = this.Right.Value;
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
                        return (passedValue == this.Right.Value) == false;
                    }
                }
            }
            else
            {
                throw new Exception("Unsupprted condition type.");
            }

            return false;
        }
    }
}
