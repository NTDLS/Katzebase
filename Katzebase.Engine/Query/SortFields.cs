using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.Engine.Query.Tokenizers
{
    public class SortFields : PrefixedFields
    {
        public KbSortDirection SortDirection { get; set; }

        public SortField Add(string key, KbSortDirection sortDirection)
        {
            string prefix = string.Empty;
            string field = key;

            if (key.Contains('.'))
            {
                var parts = key.Split('.');
                prefix = parts[0];
                field = parts[1];
            }

            var newField = new SortField(prefix, field)
            {
                Ordinal = this.Count,
                SortDirection = sortDirection
            };
            this.Add(newField);

            return newField;
        }
    }
}
