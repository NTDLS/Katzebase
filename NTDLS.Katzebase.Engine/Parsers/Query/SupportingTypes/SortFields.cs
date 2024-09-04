using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
{
    public class SortFields : PrefixedFields
    {
        public KbSortDirection SortDirection { get; private set; }

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
                Ordinal = Count,
                SortDirection = sortDirection
            };
            Add(newField);

            return newField;
        }
    }
}
