using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class OrderByFieldCollection : QueryFieldCollection
    {
        public KbSortDirection SortDirection { get; internal set; }

        public OrderByFieldCollection(QueryBatch queryBatch)
            :base(queryBatch)
        {
        }
    }
}
