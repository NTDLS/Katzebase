namespace NTDLS.Katzebase.Engine.Functions.Aggregate.Parameters
{
    internal class AggregateStringArrayParameter : AggregateGenericParameter
    {
        public List<string> Values { get; set; } = new();
    }
}
