using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Parsers.Fields
{
    /// <summary>
    /// Contains the highest level of query fields, these are things like a select list or update list.
    /// </summary>
    public class QueryField(string alias, int ordinal, IQueryField expression)
    {
        /// <summary>
        /// Alias of the expression, such as "SELECT 10+10 as Salary", Alias would contain "Salary".
        /// </summary>
        public string Alias { get; set; } = alias;

        public int Ordinal { get; set; } = ordinal;

        /// <summary>
        /// Only used for Order By fields.
        /// </summary>
        public KbSortDirection SortDirection { get; internal set; }

        /// <summary>
        /// Contains an instance that defines the value of the query field (could be a string, number, string or numeric expression, or a function call).
        /// </summary>
        public IQueryField Expression { get; set; } = expression;
    }
}

