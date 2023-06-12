using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.Engine.Query
{
    public class SortField : PrefixedField
    {
        public KbSortDirection SortDirection { get; set; } = KbSortDirection.Ascending;

        public SortField(string prefix, string field)
            : base(prefix, field)
        {
        }

        public SortField(string prefix, string field, string alias)
            : base(prefix, field, alias)
        {
        }
    }
}
