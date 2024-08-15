using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Shared;
using System.Runtime.CompilerServices;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Query.Constraints
{
    internal class Condition
    {
        public string ConditionKey { get; private set; }
        public SmartValue Left { get; private set; } = new();
        public SmartValue Right { get; private set; } = new();

        /// <summary>
        /// Used by ConditionOptimization.BuildTree() do determine when an index has already been matched to this condition.
        /// </summary>
        public bool IsIndexOptimized { get; set; } = false;

        /// <summary>
        /// Logical connector: AND or OR
        /// </summary>
        public LogicalConnector LogicalConnector { get; private set; } = LogicalConnector.None;
        /// <summary>
        /// Logical qualifier: Equals, greater than, not equal, etc.
        /// </summary>
        public LogicalQualifier LogicalQualifier { get; private set; } = LogicalQualifier.None;

        public Condition(string conditionKey, LogicalConnector logicalConnector,
            string left, LogicalQualifier logicalQualifier, string right)
        {
            ConditionKey = conditionKey;
            Left.Value = left;
            Right.Value = right;
            LogicalConnector = logicalConnector;
            LogicalQualifier = logicalQualifier;
        }

        public Condition(string conditionKey,
            LogicalConnector logicalConnector, LogicalQualifier logicalQualifier)
        {
            ConditionKey = conditionKey;
            LogicalConnector = logicalConnector;
            LogicalQualifier = logicalQualifier;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Condition other)
            {
                return Left.Key.Equals(other.Left.Key);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Left.Key.GetHashCode();
        }

        /// <summary>
        /// Swaps the left and right values.
        /// </summary>
        public void Invert()
        {
            (Right, Left) = (Left, Right);
        }
        public Condition Clone()
        {
            var clone = new Condition(ConditionKey, LogicalConnector, LogicalQualifier)
            {
                Left = Left.Clone(),
                Right = Right.Clone()
            };

            return clone;
        }

        public bool IsMatch(Transaction transaction, string? passedValue)
        {
            return IsMatch(transaction, passedValue, LogicalQualifier, Right.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreaterOrEqual(Transaction transaction, double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left >= right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesserOrEqual(Transaction transaction, double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left <= right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreaterOrEqual(Transaction transaction, string? left, string? right)
        {
            if (left != null && right != null && int.TryParse(left, out var iLeft))
            {
                if (double.TryParse(right, out var iRight))
                {
                    return iLeft >= iRight;
                }
            }

            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesserOrEqual(Transaction transaction, string? left, string? right)
        {
            if (left != null && right != null && int.TryParse(left, out var iLeft))
            {
                if (double.TryParse(right, out var iRight))
                {
                    return iLeft <= iRight;
                }

            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreater(Transaction transaction, double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left > right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesser(Transaction transaction, double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left < right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreater(Transaction transaction, string? left, string? right)
        {
            if (left != null && right != null && int.TryParse(left, out var iLeft))
            {
                if (double.TryParse(right, out var iRight))
                {
                    return iLeft > iRight;
                }

            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesser(Transaction transaction, string? left, string? right)
        {
            if (left != null && right != null && int.TryParse(left, out var iLeft))
            {
                if (double.TryParse(right, out var iRight))
                {
                    return iLeft < iRight;
                }
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLike(Transaction transaction, string? input, string? pattern)
        {
            if (input == null || pattern == null)
            {
                transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
                return null;
            }
            return input.IsLike(pattern);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchBetween(Transaction transaction, double? value, double? rangeLow, double? rangeHigh)
        {
            if (value == null || rangeLow == null || rangeHigh == null)
            {
                transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            }

            return value >= rangeLow && value <= rangeHigh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchBetween(Transaction transaction, string? input, string? pattern)
        {
            if (input == null || pattern == null)
            {
                transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
                return null;
            }

            var range = pattern.Split(':');

            double value = 0;

            if (input != string.Empty)
            {
                if (!double.TryParse(input, out value))
                {
                    throw new KbEngineException("Value could not be converted to double.");
                }
            }
            if (!double.TryParse(range[0], out var rangeLeft))
            {
                throw new KbEngineException("Left of range could not be converted to double.");
            }
            if (!double.TryParse(range[1], out var rangeRight))
            {
                throw new KbEngineException("Right of range could not be converted to double.");
            }

            return value >= rangeLeft && value <= rangeRight;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchEqual(Transaction transaction, string? left, string? right)
        {
            return left == right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMatch(Transaction transaction, string? leftString, LogicalQualifier logicalQualifier, string? rightString)
        {
            if (logicalQualifier == LogicalQualifier.Equals)
            {
                return IsMatchEqual(transaction, leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.NotEquals)
            {
                return IsMatchEqual(transaction, leftString, rightString) == false;
            }
            else if (logicalQualifier == LogicalQualifier.GreaterThan)
            {
                return IsMatchGreater(transaction, leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.LessThan)
            {
                return IsMatchLesser(transaction, leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.GreaterThanOrEqual)
            {
                return IsMatchGreaterOrEqual(transaction, leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.LessThanOrEqual)
            {
                return IsMatchLesserOrEqual(transaction, leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.Like)
            {
                return IsMatchLike(transaction, leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.NotLike)
            {
                return IsMatchLike(transaction, leftString, rightString) == false;
            }
            else if (logicalQualifier == LogicalQualifier.Between)
            {
                return IsMatchBetween(transaction, leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.NotBetween)
            {
                return IsMatchBetween(transaction, leftString, rightString) == false;
            }
            else
            {
                throw new KbParserException("Unsupported condition type.");
            }
        }
    }
}
