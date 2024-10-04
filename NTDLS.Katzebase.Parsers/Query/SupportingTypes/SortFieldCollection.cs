using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class SortFieldCollection : QueryFieldCollection
    {
        public KbSortDirection SortDirection { get; internal set; }

        public SortFieldCollection(QueryBatch queryBatch)
            :base(queryBatch)
        {
        }
    }
}
