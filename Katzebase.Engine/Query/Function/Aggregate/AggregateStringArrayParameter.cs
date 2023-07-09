namespace Katzebase.Engine.Query.Function.Aggregate
{
    internal class AggregateStringArrayParameter : AggregateGenericParameter
    {
        public List<string> Values { get; set; } = new();
    }
}
