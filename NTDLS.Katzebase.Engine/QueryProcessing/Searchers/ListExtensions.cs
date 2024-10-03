using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal static class ListExtensions
    {
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
                throw new KbProcessingException($"Ambiguous field [{fieldNameForException}].");
            }

            list[ordinal] = value;
        }
    }
}
