using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal static class SearcherExtensions
    {
        public static KbInsensitiveDictionary<string?> Flatten(this KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> collection)
        {
            var results = new KbInsensitiveDictionary<string?>();

            foreach (var schema in collection)
            {
                foreach (var values in schema.Value)
                {
                    results.Add($"{schema.Key}.{values.Key}".TrimStart(['.']), values.Value);
                }
            }

            return results;
        }

        public static void InsertWithPadding(this List<string?> list, string fieldNameForException, int ordinal, string? value)
        {
            if (list.Count <= ordinal)
            {
                int difference = ordinal + 1 - list.Count;
                if (difference > 0)
                {
                    list.AddRange(new string[difference]);
                }
            }
            if (list[ordinal] != null)
            {
                throw new KbProcessingException($"Ambiguous field: [{fieldNameForException}].");
            }

            list[ordinal] = value;
        }
    }
}
