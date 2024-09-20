using NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions;
using static NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions.ExpressionConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Exposed
{
    /// <summary>
    /// The "exposed" classes are helpers that allow us to access the ordinal of fields as well as the some of the nester properties.
    /// This one is for expression fields, and their ordinals.
    /// </summary>
    internal class ExposedExpression
    {
        public int Ordinal { get; set; }
        public string FieldAlias { get; set; }
        public IQueryFieldExpression FieldExpression { get; set; }
        public CollapseType CollapseType { get; set; }

        public ExposedExpression(int ordinal, string fieldAlias, IQueryFieldExpression fieldExpression, CollapseType collapseType)
        {
            Ordinal = ordinal;
            FieldAlias = fieldAlias;
            FieldExpression = fieldExpression;
            CollapseType = collapseType;
        }
    }
}
