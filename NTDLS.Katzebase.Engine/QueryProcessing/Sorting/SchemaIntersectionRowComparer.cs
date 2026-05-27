using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Sorting
{
    internal static class MaterializedRowComparer
    {
        /// <summary>
        /// Compares two materialized rows by their ORDER BY columns in sequence.
        ///
        /// Numeric fields are compared as doubles; non-numeric fields fall back to
        /// case-insensitive string comparison.  The numeric values are pre-parsed at
        /// result-build time (O(n) total) so this method never calls TryParse — the
        /// sort itself stays at O(n log n) comparisons with no parse overhead.
        /// </summary>
        public static int Compare(List<(string fieldAlias, KbSortDirection sortDirection)> sortingColumns, MaterializedRow? x, MaterializedRow? y)
        {
            foreach (var (fieldAlias, sortDirection) in sortingColumns)
            {
                double? xNum = null;
                double? yNum = null;

                // TryGetValue rather than the indexer — an absent key means the field was
                // not populated for this row (e.g. a missing aggregate), which we treat as
                // non-numeric and fall through to string comparison.
                x?.OrderByNumericValues.TryGetValue(fieldAlias, out xNum);
                y?.OrderByNumericValues.TryGetValue(fieldAlias, out yNum);

                // Use numeric comparison when both sides parsed successfully; this
                // prevents "9000000" from sorting after "28000000" as strings would.
                int result = (xNum.HasValue && yNum.HasValue)
                    ? xNum.Value.CompareTo(yNum.Value)
                    : string.Compare(x?.OrderByValues[fieldAlias], y?.OrderByValues?[fieldAlias], StringComparison.OrdinalIgnoreCase);

                if (result != 0)
                    return sortDirection == KbSortDirection.Descending ? -result : result;
            }
            return 0;
        }
    }
}
