using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
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
