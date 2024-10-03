using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;
using static NTDLS.Katzebase.Parsers.Query.Fields.Expressions.ExpressionConstants;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.Exposed
{
    /// <summary>
    /// The "exposed" classes are helpers that allow us to access the ordinal of fields as well as the some of the nester properties.
    /// This one is for expression fields, and their ordinals.
    /// </summary>
    public class ExposedExpression<TData> where TData : IStringable
    {
        public int Ordinal { get; set; }
        public string FieldAlias { get; set; }
        public IQueryFieldExpression<TData> FieldExpression { get; set; }
        public CollapseType CollapseType { get; set; }

        public ExposedExpression(int ordinal, string fieldAlias, IQueryFieldExpression<TData> fieldExpression, CollapseType collapseType)
        {
            Ordinal = ordinal;
            FieldAlias = fieldAlias.ToLowerInvariant();
            FieldExpression = fieldExpression;
            CollapseType = collapseType;
        }
    }
}
