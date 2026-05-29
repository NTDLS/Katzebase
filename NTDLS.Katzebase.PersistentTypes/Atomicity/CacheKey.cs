namespace NTDLS.Katzebase.PersistentTypes.Atomicity
{
    public class CacheKey(string value)
    {
        public string Value { get; private set; } = value.ToLowerInvariant();

        public override string ToString() => Value;
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object? obj)
        {
            if (obj is CacheKey other)
            {
                return string.Equals(Value, other.Value);
            }
            return false;
        }
    }
}
