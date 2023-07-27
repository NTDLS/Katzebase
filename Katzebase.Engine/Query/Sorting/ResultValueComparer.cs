using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.Engine.Query.Sorting
{
    public class ResultValueComparer : IComparer<Dictionary<string, string?>>
    {
        private readonly List<(string fieldName, KbSortDirection direction)> sortingColumns;

        public ResultValueComparer(List<(string fieldName, KbSortDirection sortDirection)> sortingColumns)
        {
            this.sortingColumns = sortingColumns;
        }

        public int Compare(Dictionary<string, string?>? x, Dictionary<string, string?>? y)
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
