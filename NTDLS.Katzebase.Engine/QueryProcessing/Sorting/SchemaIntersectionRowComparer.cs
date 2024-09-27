using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Sorting
{
    internal static class SchemaIntersectionRowComparer
    {
        public static int Compare<TData>(List<(string fieldName, KbSortDirection sortDirection)> sortingColumns, SchemaIntersectionRow<TData>? x, SchemaIntersectionRow<TData>? y)
            where TData : IStringable
        {
            foreach (var (fieldName, sortDirection) in sortingColumns)
            {
                int result = EngineCore<TData>.Compare(x.AuxiliaryFields[fieldName], y.AuxiliaryFields[fieldName]); //, StringComparison.OrdinalIgnoreCase);

                if (result != 0)
                {
                    return sortDirection == KbSortDirection.Descending ? -result : result;
                }
            }
            return 0;
        }
    }
}
