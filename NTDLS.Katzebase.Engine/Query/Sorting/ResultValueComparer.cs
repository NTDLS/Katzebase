using Katzebase.Types;
using static Katzebase.KbConstants;

namespace Katzebase.Engine.Query.Sorting
{
    public class ResultValueComparer : IComparer<KbInsensitiveDictionary<string?>>
    {
        private readonly List<(string fieldName, KbSortDirection direction)> sortingColumns;

        public ResultValueComparer(List<(string fieldName, KbSortDirection sortDirection)> sortingColumns)
        {
            this.sortingColumns = sortingColumns;
        }

        public int Compare(KbInsensitiveDictionary<string?>? x, KbInsensitiveDictionary<string?>? y)
        {
            foreach (var (fieldName, sortDirection) in sortingColumns)
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
