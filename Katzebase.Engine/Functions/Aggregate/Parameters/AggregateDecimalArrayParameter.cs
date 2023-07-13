namespace Katzebase.Engine.Functions.Aggregate.Parameters
{
    internal class AggregateDecimalArrayParameter : AggregateGenericParameter
    {
        public List<decimal> Values { get; set; } = new();
    }
}
