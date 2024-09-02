using NTDLS.Katzebase.Engine.Parsers.Query.Fields;

namespace NTDLS.Katzebase.Engine.Parsers.Query
{
    /// <summary>
    /// Contains the highest level of query fields, these are things like a select list or update list.
    /// </summary>
    public class QueryField
    {
        /// <summary>
        /// Alias of the expression, such as "SELECT 10+10 as Salary", Alias would contain "Salary".
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Contains an instance that defines the value of the query field (could be a string, number, string or numeric expression, or a function call).
        /// </summary>
        public IQueryField Expression { get; set; }

        public QueryField(string name, IQueryField expression)
        {
            Alias = name;
            Expression = expression;
        }
    }
}
