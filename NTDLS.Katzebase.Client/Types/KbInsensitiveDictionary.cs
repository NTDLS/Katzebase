namespace NTDLS.Katzebase.Client.Types
{
    /// <summary>
    /// The katzebase Case-insensitive lookup.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    [Serializable]
    public class KbInsensitiveDictionary<TValue> : Dictionary<string, TValue>
    {
        public KbInsensitiveDictionary() : base(StringComparer.InvariantCultureIgnoreCase) { }

        public KbInsensitiveDictionary<TValue> Clone()
        {
            var clone = new KbInsensitiveDictionary<TValue>();
            foreach (var source in this)
            {
                clone.Add(source.Key, source.Value);
            }
            return clone;
        }
    }
}
