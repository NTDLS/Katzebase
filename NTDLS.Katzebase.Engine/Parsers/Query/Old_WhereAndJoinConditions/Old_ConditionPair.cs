using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
{
    /// <summary>
    /// Used when parsing a condition, contains the left and right value along with the comparison operator.
    /// </summary>
    internal class Old_ConditionPair
    {
        public IQueryField Right { get; set; }
        public LogicalQualifier Qualifier { get; set; }
        public IQueryField Left { get; set; }

        /// <summary>
        /// The name of the variable in ConditionCollection.MathematicalExpression that is represented by this condition.
        /// </summary>
        public string ExpressionVariable { get; set; }

        public Old_ConditionPair(string expressionVariable, IQueryField left, LogicalQualifier qualifier, IQueryField right)
        {
            ExpressionVariable = expressionVariable;
            Left = left;
            Qualifier = qualifier;
            Right = right;
        }
    }
}
