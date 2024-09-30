using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class SortField : PrefixedField
    {
        public KbSortDirection SortDirection { get; set; } = KbSortDirection.Ascending;

        public SortField(int? scriptLine, string prefix, string field)
            : base(scriptLine, prefix, field)
        {
        }

        public SortField(int? scriptLine, string prefix, string field, string alias)
            : base(scriptLine, prefix, field, alias)
        {
        }
    }
}
