namespace ParserV2.Expression
{
    /// <summary>
    /// Contains the highest level of expression, these are things like a select list or update list.
    /// </summary>
    internal class NamedExpression
    {
        /// <summary>
        /// Alias of the expression, such as "SELECT 10+10 as Salary", Alias would contain "Salary".
        /// </summary>
        public string Alias { get; set; }

        public IExpression Expression { get; set; }

        public NamedExpression(string name, IExpression expression)
        {
            Alias = name;
            Expression = expression;
        }
    }
}
