using Katzebase.Engine.Documents;
using Katzebase.Library;
using Katzebase.Library.Exceptions;
using Newtonsoft.Json.Linq;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class ConditionSingle : ICondition
    {
        public ConditionValue Left { get; set; } = new ConditionValue();
        public ConditionValue Right { get; set; } = new ConditionValue();
        public LogicalQualifier LogicalQualifier { get; set; } = LogicalQualifier.None;
        public LogicalConnector LogicalConnector { get; set; } = LogicalConnector.None;
        public bool CoveredByIndex { get; set; }

        public ICondition Clone()
        {
            return new ConditionSingle()
            {
                CoveredByIndex = CoveredByIndex,
                LogicalQualifier = LogicalQualifier,
                LogicalConnector = LogicalConnector,
                Left = Left.Clone(),
                Right = Right.Clone()
            };
        }

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

        /*
        public bool IsMatch(PersistDocument persistDocument)
        {
            Utility.EnsureNotNull(persistDocument);
            Utility.EnsureNotNull(persistDocument.Content);

            JObject jsonContent = JObject.Parse(persistDocument.Content);

            return IsMatch(jsonContent);
        }

        public bool IsMatch(JObject jsonContent)
        {
            bool fullAttributeMatch = true;

            //Loop though each condition in the prepared query:
            foreach (var condition in Collection)
            {
                //Get the value of the condition:
                if (jsonContent.TryGetValue(condition.Field, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                {
                    //If the condition does not match the value in the document then we break from checking the remainder of the conditions for this document and continue with the next document.
                    //Otherwise we continue to the next condition until all conditions are matched.
                    if (condition.IsMatch(jToken.ToString().ToLower()) == false)
                    {
                        fullAttributeMatch = false;
                        break;
                    }
                }
            }

            return fullAttributeMatch;
        }
        */

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
