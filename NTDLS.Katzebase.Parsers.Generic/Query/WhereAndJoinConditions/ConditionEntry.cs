using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Katzebase.Parsers.Query.Fields;
//using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Interfaces;
using System.Runtime.CompilerServices;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Parsers.Constants;


namespace NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions
{
    public class ConditionEntry<TData> : ICondition where TData : IStringable
    {
        #region Internal classes.

        /// <summary>
        /// Used when parsing a condition, contains the left and right value along with the comparison operator.
        /// </summary>
        public class ConditionValuesPair<TData1> where TData1 : IStringable
        {
            public IQueryField<TData1> Right { get; set; }
            public LogicalQualifier Qualifier { get; set; }
            public IQueryField<TData1> Left { get; set; }

            /// <summary>
            /// The name of the variable in ConditionCollection.MathematicalExpression that is represented by this condition.
            /// </summary>
            public string ExpressionVariable { get; set; }

            public ConditionValuesPair(string expressionVariable, IQueryField<TData1> left, LogicalQualifier qualifier, IQueryField<TData1> right)
            {
                ExpressionVariable = expressionVariable;
                Left = left;
                Qualifier = qualifier;
                Right = right;
            }
        }

        #endregion

        public string ExpressionVariable { get; set; }
        public IQueryField<TData> Left { get; set; }
        public LogicalQualifier Qualifier { get; set; }
        public IQueryField<TData> Right { get; set; }

        /// <summary>
        /// Used by ConditionOptimization.BuildTree() do determine when an index has already been matched to this condition.
        /// </summary>
        public bool IsIndexOptimized { get; set; } = false;

        public ConditionEntry(ConditionValuesPair<TData> pair)
        {
            ExpressionVariable = pair.ExpressionVariable;
            Left = pair.Left;
            Qualifier = pair.Qualifier;
            Right = pair.Right;
        }

        public ConditionEntry(string expressionVariable, IQueryField<TData> left, LogicalQualifier qualifier, IQueryField<TData> right)
        {
            ExpressionVariable = expressionVariable;
            Left = left;
            Qualifier = qualifier;
            Right = right;
        }

        public ICondition Clone()
        {
            return new ConditionEntry<TData>(ExpressionVariable, Left.Clone(), Qualifier, Right.Clone());
        }

        public bool IsMatch<TData>(ITransaction<TData> transaction, string? collapsedLeft, string? collapsedRight) where TData : IStringable
        {
            return IsMatch(transaction, collapsedLeft, Qualifier, collapsedRight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreaterOrEqual<TData>(ITransaction<TData> transaction, double? left, double? right) where TData : IStringable
        {
            if (left != null && right != null)
            {
                return left >= right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesserOrEqual<TData>(ITransaction<TData> transaction, double? left, double? right) where TData : IStringable
        {
            if (left != null && right != null)
            {
                return left <= right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreaterOrEqual<TData>(ITransaction<TData> transaction, string? left, string? right) where TData : IStringable
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
        public static bool? IsMatchLesserOrEqual<TData>(ITransaction<TData> transaction, string? left, string? right) where TData : IStringable
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
        public static bool? IsMatchGreater<TData>(ITransaction<TData> transaction, double? left, double? right) where TData : IStringable
        {
            if (left != null && right != null)
            {
                return left > right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchLesser<TData>(ITransaction<TData> transaction, double? left, double? right) where TData : IStringable
        {
            if (left != null && right != null)
            {
                return left < right;
            }
            transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchGreater<TData>(ITransaction<TData> transaction, string? left, string? right) where TData : IStringable
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
        public static bool? IsMatchLesser<TData>(ITransaction<TData> transaction, string? left, string? right) where TData : IStringable
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
        public static bool? IsMatchLike<TData>(ITransaction<TData> transaction, string? input, string? pattern) where TData : IStringable
        {
            if (input == null || pattern == null)
            {
                transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
                return null;
            }
            return input.IsLike(pattern);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchBetween<TData>(ITransaction<TData> transaction, double? value, double? rangeLow, double? rangeHigh) where TData : IStringable 
        {
            if (value == null || rangeLow == null || rangeHigh == null)
            {
                transaction.AddWarning(KbTransactionWarning.ResultDisqualifiedByNullValue);
            }

            return value >= rangeLow && value <= rangeHigh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? IsMatchBetween<TData>(ITransaction<TData> transaction, string? input, string? pattern) where TData : IStringable
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
        public static bool? IsMatchEqual<TData>(ITransaction<TData> transaction, string? left, string? right) where TData : IStringable
        {
            return left == right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMatch<TData>(ITransaction<TData> transaction, string? leftString, LogicalQualifier logicalQualifier, string? rightString) where TData : IStringable
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
