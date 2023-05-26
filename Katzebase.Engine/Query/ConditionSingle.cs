using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class ConditionSingle : ConditionBase
    {
        public string Field { get; set; } = string.Empty;
        public bool IsKeyConstant { get; set; } = false;
        public string Value { get; set; } = string.Empty;
        public bool IsValueConstant { get; set; } = false;
        public LogicalQualifier LogicalQualifier { get; set; } = LogicalQualifier.None;
        public LogicalConnector LogicalConnector { get; set; } = LogicalConnector.None;

        public ConditionSingle(LogicalConnector logicalConnector, string key)
        {
            LogicalConnector = logicalConnector;
            Field = key;
        }

        public ConditionSingle(LogicalConnector logicalConnector)
        {
            LogicalConnector = logicalConnector;
        }

        public ConditionSingle(LogicalConnector logicalConnector, string key, LogicalQualifier logicalQualifier, string value)
        {
            this.Field = key;
            this.Value = value;
            this.LogicalQualifier = logicalQualifier;
            this.LogicalConnector = logicalConnector;
        }

        public bool IsMatch(string passedValue)
        {
            if (this.LogicalQualifier == LogicalQualifier.Equals)
            {
                return (passedValue == (string)this.Value);
            }
            else if (this.LogicalQualifier == LogicalQualifier.NotEquals)
            {
                return (passedValue != (string)this.Value);
            }
            else if (this.LogicalQualifier == LogicalQualifier.GreaterThan)
            {
                decimal left;
                decimal right;
                if (Decimal.TryParse(passedValue, out left) && Decimal.TryParse((string)this.Value, out right))
                {
                    return (left > right);
                }
            }
            else if (this.LogicalQualifier == LogicalQualifier.LessThan)
            {
                decimal left;
                decimal right;
                if (Decimal.TryParse(passedValue, out left) && Decimal.TryParse((string)this.Value, out right))
                {
                    return (left < right);
                }
            }
            else if (this.LogicalQualifier == LogicalQualifier.GreaterThanOrEqual)
            {
                decimal left;
                decimal right;
                if (Decimal.TryParse(passedValue, out left) && Decimal.TryParse((string)this.Value, out right))
                {
                    return (left >= right);
                }
            }
            else if (this.LogicalQualifier == LogicalQualifier.LessThanOrEqual)
            {
                decimal left;
                decimal right;
                if (Decimal.TryParse(passedValue, out left) && Decimal.TryParse((string)this.Value, out right))
                {
                    return (left <= right);
                }
            }
            else if (this.LogicalQualifier == LogicalQualifier.Like)
            {
                string right = (string)this.Value;

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
                    return (passedValue == (string)this.Value);
                }
            }
            else if (this.LogicalQualifier == LogicalQualifier.NotLike)
            {
                string right = (string)this.Value;

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
                    return (passedValue == (string)this.Value) == false;
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
