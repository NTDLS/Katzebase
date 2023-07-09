namespace Katzebase.Engine.Query.Function.Aggregate
{
    internal class AggregateDecimalArrayParameter: AggregateGenericParameter
    {
        public List<decimal> Values { get; set; } = new();
    }
}
