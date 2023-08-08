namespace Katzebase.PublicLibrary.Payloads
{
    /// <summary>
    /// The katzebase Case-insensitive lookup.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    [Serializable]
    public class KBCILookup<TValue> : Dictionary<string, TValue>
    {
        public KBCILookup() : base(StringComparer.InvariantCultureIgnoreCase) { }
    }
}
