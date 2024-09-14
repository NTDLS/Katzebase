using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
{
    internal class ConditionLeftAndRight
    {
        public IQueryField Right { get; set; }
        public LogicalQualifier Qualifier { get; set; }
        public IQueryField Left { get; set; }

        public string ExpressionVariable { get; set; }

        public ConditionLeftAndRight(string expressionVariable, IQueryField left, LogicalQualifier qualifier, IQueryField right)
        {
            ExpressionVariable = expressionVariable;
            Left = left;
            Qualifier = qualifier;
            Right = right;
        }
    }
}
