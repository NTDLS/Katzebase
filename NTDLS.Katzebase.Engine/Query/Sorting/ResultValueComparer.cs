using NTDLS.Katzebase.Types;
using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Engine.Query.Sorting
{
    public class ResultValueComparer : IComparer<KbInsensitiveDictionary<string?>>
    {
        private readonly List<(string fieldName, KbSortDirection direction)> _sortingColumns;

        public ResultValueComparer(List<(string fieldName, KbSortDirection sortDirection)> sortingColumns)
        {
            _sortingColumns = sortingColumns;
        }

        public int Compare(KbInsensitiveDictionary<string?>? x, KbInsensitiveDictionary<string?>? y)
        {
            foreach (var (fieldName, sortDirection) in _sortingColumns)
            {
                //if (fieldName >= x?.Count || fieldName >= y?.Count)
                //    return 0;

                int result = string.Compare(x?[fieldName], y?[fieldName], StringComparison.OrdinalIgnoreCase);

                if (result != 0)
                {
                    return sortDirection == KbSortDirection.Descending ? -result : result;
                }
            }

            return 0;
        }
    }
}
