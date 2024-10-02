using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Katzebase.Parsers.Query.Fields;
using System.Runtime.CompilerServices;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions
{
    public class ConditionEntry : ICondition
    {
        #region public classes.

        /// <summary>
        /// Used when parsing a condition, contains the left and right value along with the comparison operator.
        /// </summary>
        public class ConditionValuesPair
        {
            public IQueryField Right { get; set; }
            public LogicalQualifier Qualifier { get; set; }
            public IQueryField Left { get; set; }

            /// <summary>
            /// The name of the variable in ConditionCollection.MathematicalExpression that is represented by this condition.
            /// </summary>
            public string ExpressionVariable { get; set; }

            public ConditionValuesPair(string expressionVariable, IQueryField left, LogicalQualifier qualifier, IQueryField right)
            {
                ExpressionVariable = expressionVariable;
                Left = left;
                Qualifier = qualifier;
                Right = right;
            }
        }

        #endregion

        public string ExpressionVariable { get; set; }
        public IQueryField Left { get; set; }
        public LogicalQualifier Qualifier { get; set; }
        public IQueryField Right { get; set; }

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
        }

        public ConditionEntry(string expressionVariable, IQueryField left, LogicalQualifier qualifier, IQueryField right)
        {
            ExpressionVariable = expressionVariable;
            Left = left;
            Qualifier = qualifier;
            Right = right;
        }

        public ICondition Clone()
        {
            return new ConditionEntry(ExpressionVariable, Left.Clone(), Qualifier, Right.Clone());
        }

        public bool IsMatch(ITransaction transaction, string? collapsedLeft, string? collapsedRight)
        {
            return IsMatch(transaction, collapsedLeft, Qualifier, collapsedRight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreaterOrEqual(ITransaction transaction, double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left >= right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesserOrEqual(ITransaction transaction, double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left <= right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreaterOrEqual(ITransaction transaction, string? left, string? right)
        {
            if (left != null && right != null && double.TryParse(left, out var iLeft))
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
        public static bool? IsMatchLesserOrEqual(ITransaction transaction, string? left, string? right)
        {
            if (left != null && right != null && double.TryParse(left, out var iLeft))
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
        public static bool? IsMatchGreater(ITransaction transaction, double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left > right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesser(ITransaction transaction, double? left, double? right)
        {
            if (left != null && right != null)
            {
                return left < right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreater(ITransaction transaction, string? left, string? right)
        {
            if (left != null && right != null && double.TryParse(left, out var iLeft))
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
        public static bool? IsMatchLesser(ITransaction transaction, string? left, string? right)
        {
            if (left != null && right != null && double.TryParse(left, out var iLeft))
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
        public static bool? IsMatchLike(ITransaction transaction, string? input, string? pattern)
        {
            if (input == null || pattern == null)
            {
                transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
                return null;
            }
            return input.IsLike(pattern);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchBetween(ITransaction transaction, double? value, double? rangeLow, double? rangeHigh)
        {
            if (value == null || rangeLow == null || rangeHigh == null)
            {
                transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            }

            return value >= rangeLow && value <= rangeHigh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchBetween(ITransaction transaction, string? input, string? pattern)
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
                    throw new KbProcessingException("Value could not be converted to double.");
                }
            }
            if (!double.TryParse(range[0], out var rangeLeft))
            {
                throw new KbProcessingException("Left of range could not be converted to double.");
            }
            if (!double.TryParse(range[1], out var rangeRight))
            {
                throw new KbProcessingException("Right of range could not be converted to double.");
            }

            return value >= rangeLeft && value <= rangeRight;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchEqual(ITransaction transaction, string? left, string? right)
        {
            return left == right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMatch(ITransaction transaction, string? leftString, LogicalQualifier logicalQualifier, string? rightString)
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
                throw new KbNotImplementedException("Condition type is not implemented.");
            }
        }
    }
}
