namespace Katzebase.Engine.Query.QueryField
{
    internal class QueryFieldMethodAndParams : QueryFieldParameterBase
    {
        public string Method { get; set; } = string.Empty;
        public List<QueryFieldParameterBase> Parameters { get; set; } = new();
    }
}
