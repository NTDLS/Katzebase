using System;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class Condition
    {
        public string Key { get; set; }
        public bool IsKeyConstant { get; set; } = false;
        public string Value { get; set; } = string.Empty;
        public bool IsValueConstant { get; set; } = false;
        public ConditionQualifier ConditionQualifier { get; set; } = ConditionQualifier.None;
        public ConditionType ConditionType { get; set; } = ConditionType.None;

        public Condition(ConditionType conditionType, string key)
        {
            ConditionType = conditionType;
            Key = key;
        }

        public Condition(ConditionType conditionType, string key, ConditionQualifier conditionQualifier, string value)
        {
            this.Key = key;
            this.Value = value;
            this.ConditionQualifier = conditionQualifier;
            this.ConditionType = conditionType;
        }

        public bool IsMatch(string passedValue)
        {
            if (this.ConditionQualifier == ConditionQualifier.Equals)
            {
                return (passedValue == (string)this.Value);
            }
            else if (this.ConditionQualifier == ConditionQualifier.NotEquals)
            {
                return (passedValue != (string)this.Value);
            }
            else if (this.ConditionQualifier == ConditionQualifier.GreaterThan)
            {
                decimal left;
                decimal right;
                if (Decimal.TryParse(passedValue, out left) && Decimal.TryParse((string)this.Value, out right))
                {
                    return (left > right);
                }
            }
            else if (this.ConditionQualifier == ConditionQualifier.LessThan)
            {
                decimal left;
                decimal right;
                if (Decimal.TryParse(passedValue, out left) && Decimal.TryParse((string)this.Value, out right))
                {
                    return (left < right);
                }
            }
            else if (this.ConditionQualifier == ConditionQualifier.GreaterThanOrEqual)
            {
                decimal left;
                decimal right;
                if (Decimal.TryParse(passedValue, out left) && Decimal.TryParse((string)this.Value, out right))
                {
                    return (left >= right);
                }
            }
            else if (this.ConditionQualifier == ConditionQualifier.LessThanOrEqual)
            {
                decimal left;
                decimal right;
                if (Decimal.TryParse(passedValue, out left) && Decimal.TryParse((string)this.Value, out right))
                {
                    return (left <= right);
                }
            }
            else if (this.ConditionQualifier == ConditionQualifier.Like)
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
            else if (this.ConditionQualifier == ConditionQualifier.NotLike)
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
