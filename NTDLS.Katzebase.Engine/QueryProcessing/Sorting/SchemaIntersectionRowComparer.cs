using NTDLS.Katzebase.Engine.QueryProcessing.Searchers;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Sorting
{
    internal static class MaterializedRowComparer
    {
        public static int Compare(List<(string fieldAlias, KbSortDirection sortDirection)> sortingColumns, MaterializedRow? x, MaterializedRow? y)
        {
            foreach (var (fieldAlias, sortDirection) in sortingColumns)
            {
                int result = string.Compare(x?.OrderByValues[fieldAlias], y?.OrderByValues?[fieldAlias], StringComparison.OrdinalIgnoreCase);

                if (result != 0)
                {
                    return sortDirection == KbSortDirection.Descending ? -result : result;
                }
            }
            return 0;
        }
    }
}
