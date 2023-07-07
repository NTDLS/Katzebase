namespace Katzebase.Engine.Query.Function.Procedures
{
    internal class QueryProcedureParameterValue
    {
        public QueryProcedureParameterPrototype Parameter { get; set; }
        public string? Value { get; set; } = null;

        public QueryProcedureParameterValue(QueryProcedureParameterPrototype parameter, string? value)
        {
            Parameter = parameter;
            Value = value;
        }

        public QueryProcedureParameterValue(QueryProcedureParameterPrototype parameter)
        {
            Parameter = parameter;
        }
    }
}
