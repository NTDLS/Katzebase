using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using System.Diagnostics.CodeAnalysis;

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


        public static bool TryGetMultipart(this KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> collection, string multiPartField, out string? returnValue)
        {
            var parts = multiPartField.Split('.');

            if (parts.Length == 1)
            {
                if (collection.TryGetValue(string.Empty, out var schemaElements)) //Empty schema lookup.
                {
                    if (schemaElements.TryGetValue(parts[0], out var documentValue))
                    {
                        returnValue = documentValue;
                        return true;
                    }
                }
            }
            else if (parts.Length == 2)
            {
                if (collection.TryGetValue(parts[0], out var schemaElements))
                {
                    if (schemaElements.TryGetValue(parts[1], out var documentValue))
                    {
                        returnValue = documentValue;
                        return true;
                    }
                }
            }

            returnValue = null;
            return false;
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
