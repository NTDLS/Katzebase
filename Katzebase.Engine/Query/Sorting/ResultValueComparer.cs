using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.Engine.Query.Sorting
{
    public class ResultValueComparer : IComparer<List<string>>
    {
        private readonly List<(int fieldIndex, KbSortDirection direction)> sortingColumns;

        public ResultValueComparer(List<(int fieldIndex, KbSortDirection sortDirection)> sortingColumns)
        {
            this.sortingColumns = sortingColumns;
        }

        public int Compare(List<string>? x, List<string>? y)
        {
            foreach (var (fieldIndex, sortDirection) in sortingColumns)
            {
                if (fieldIndex >= x?.Count || fieldIndex >= y?.Count)
                    return 0;

                int result = string.Compare(x?[fieldIndex], y?[fieldIndex], StringComparison.OrdinalIgnoreCase);

                if (result != 0)
                {
                    return sortDirection == KbSortDirection.Descending ? -result : result;
                }
            }

            return 0;
        }
    }
}
