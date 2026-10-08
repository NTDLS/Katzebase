using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Fields;
using System.Globalization;
using System.Runtime.CompilerServices;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Conditions
{
    public class ConditionEntry : ICondition
    {
        #region public classes.

        /// <summary>
        /// Used when parsing a condition, contains the left and right value along with the comparison operator.
        /// </summary>
        public class ConditionValuesPair(string expressionVariable, IQueryField left, LogicalQualifier qualifier, IQueryField right, IQueryField? rightHigh = null)
        {
            public IQueryField Right { get; set; } = right;

            /// <summary>
            /// For BETWEEN and NOT BETWEEN, Right is the low value of the range and this is the high value.
            /// </summary>
            public IQueryField? RightHigh { get; set; } = rightHigh;
            public LogicalQualifier Qualifier { get; set; } = qualifier;
            public IQueryField Left { get; set; } = left;

            /// <summary>
            /// The name of the variable in ConditionCollection.MathematicalExpression that is represented by this condition.
            /// </summary>
            public string ExpressionVariable { get; set; } = expressionVariable;
        }

        #endregion

        public string ExpressionVariable { get; set; }
        public IQueryField Left { get; set; }
        public LogicalQualifier Qualifier { get; set; }
        public IQueryField Right { get; set; }

        /// <summary>
        /// For BETWEEN and NOT BETWEEN, Right is the low value of the range and this is the high value.
        /// </summary>
        public IQueryField? RightHigh { get; set; }

        /// <summary>
        /// Used by ConditionOptimization.BuildTree() do determine when an index has already been matched to this condition.
        /// </summary>
        public bool IsIndexOptimized { get; set; } = false;

        public ConditionEntry(ConditionValuesPair pair)
        {
            ExpressionVariable = pair.ExpressionVariable;
            Left = pair.Left;
            Qualifier = pair.Qualifier;
            Right = pair.Right;
            RightHigh = pair.RightHigh;
        }

        public ConditionEntry(string expressionVariable, IQueryField left, LogicalQualifier qualifier, IQueryField right, IQueryField? rightHigh = null)
        {
            ExpressionVariable = expressionVariable;
            Left = left;
            Qualifier = qualifier;
            Right = right;
            RightHigh = rightHigh;
        }

        public ICondition Clone()
        {
            return new ConditionEntry(ExpressionVariable, Left.Clone(), Qualifier, Right.Clone(), RightHigh?.Clone());
        }

        public bool IsMatch(string? collapsedLeft, string? collapsedRight, string? collapsedRightHigh = null)
        {
            return IsMatch(collapsedLeft, Qualifier, collapsedRight, collapsedRightHigh);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreaterOrEqual(double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left >= right;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesserOrEqual(double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left <= right;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreaterOrEqual(string? left, string? right)
        {
            if (left != null && right != null)
            {
                if (double.TryParse(left, out var iLeft) && double.TryParse(right, out var iRight))
                {
                    return iLeft >= iRight;
                }
                throw new KbProcessingException($"IsMatchGreaterOrEqual expected numeric value, found: [{left}>={right}].");
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesserOrEqual(string? left, string? right)
        {
            if (left != null && right != null)
            {
                if (double.TryParse(left, out var iLeft) && double.TryParse(right, out var iRight))
                {
                    return iLeft <= iRight;
                }
                throw new KbProcessingException($"IsMatchLesserOrEqual expected numeric value, found: [{left}<={right}].");
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreater(double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left > right;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesser(double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left < right;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreater(string? left, string? right)
        {
            if (left != null && right != null)
            {
                if (double.TryParse(left, out var iLeft) && double.TryParse(right, out var iRight))
                {
                    return iLeft > iRight;
                }
                throw new KbProcessingException($"IsMatchGreater expected numeric value, found: [{left}>{right}].");
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesser(string? left, string? right)
        {
            if (left != null && right != null)
            {
                if (double.TryParse(left, out var iLeft) && double.TryParse(right, out var iRight))
                {
                    return iLeft < iRight;
                }
                throw new KbProcessingException($"IsMatchLesser expected numeric value, found: [{left}<{right}].");
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLike(string? input, string? pattern)
        {
            if (input == null || pattern == null)
            {
                return null;
            }
            return input.IsLike(pattern);
        }

        /// <summary>
        /// Returns whether the value is within the range, including the low and high values. Returns null when any of
        ///     the values is null, so that a missing value matches neither BETWEEN nor NOT BETWEEN.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchBetween(string? input, string? rangeLow, string? rangeHigh)
        {
            if (input == null || rangeLow == null || rangeHigh == null)
            {
                return null;
            }

            if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || !double.TryParse(rangeLow, NumberStyles.Float, CultureInfo.InvariantCulture, out var low)
                || !double.TryParse(rangeHigh, NumberStyles.Float, CultureInfo.InvariantCulture, out var high))
            {
                throw new KbProcessingException($"Between expected numeric values, found: [{input} between {rangeLow} and {rangeHigh}].");
            }

            return value >= low && value <= high;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchEqual(string? left, string? right)
        {
            return left == right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMatch(string? leftString, LogicalQualifier logicalQualifier, string? rightString, string? rightHighString = null)
        {
            if (logicalQualifier == LogicalQualifier.Equals)
            {
                return IsMatchEqual(leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.NotEquals)
            {
                return IsMatchEqual(leftString, rightString) == false;
            }
            else if (logicalQualifier == LogicalQualifier.GreaterThan)
            {
                return IsMatchGreater(leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.LessThan)
            {
                return IsMatchLesser(leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.GreaterThanOrEqual)
            {
                return IsMatchGreaterOrEqual(leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.LessThanOrEqual)
            {
                return IsMatchLesserOrEqual(leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.Like)
            {
                return IsMatchLike(leftString, rightString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.NotLike)
            {
                return IsMatchLike(leftString, rightString) == false;
            }
            else if (logicalQualifier == LogicalQualifier.Between)
            {
                return IsMatchBetween(leftString, rightString, rightHighString) == true;
            }
            else if (logicalQualifier == LogicalQualifier.NotBetween)
            {
                return IsMatchBetween(leftString, rightString, rightHighString) == false;
            }
            else
            {
                throw new KbNotImplementedException("Condition type is not implemented.");
            }
        }
    }
}
