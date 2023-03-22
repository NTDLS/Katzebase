namespace Katzebase.Engine.Query
{
    public class UpsertKeyValue
    {
        public string? Key { get; set; }
        public bool IsKeyConstant { get; set; }
        public string? Value { get; set; }
        public bool IsValueConstant { get; set; }
    }
}