using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Sorting
{
    internal static class MaterializedRowComparer
    {
        public static int Compare(List<(string fieldAlias, KbSortDirection sortDirection)> sortingColumns, MaterializedRow? x, MaterializedRow? y)
        {
            foreach (var (fieldAlias, sortDirection) in sortingColumns)
            {
                var xVal = x?.OrderByValues[fieldAlias];
                var yVal = y?.OrderByValues?[fieldAlias];

                int result;
                if (double.TryParse(xVal, out double xNum) && double.TryParse(yVal, out double yNum))
                    result = xNum.CompareTo(yNum);
                else
                    result = string.Compare(xVal, yVal, StringComparison.OrdinalIgnoreCase);

                if (result != 0)
                    return sortDirection == KbSortDirection.Descending ? -result : result;
            }
            return 0;
        }
    }
}
