using fs;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Sorting
{
    internal static class SchemaIntersectionRowComparer
    {
        public static int Compare(List<(string fieldName, KbSortDirection sortDirection)> sortingColumns, SchemaIntersectionRow? x, SchemaIntersectionRow? y)
        {
            foreach (var (fieldName, sortDirection) in sortingColumns)
            {
                int result = fstring.Compare(x?.AuxiliaryFields[fstring.NewS(fieldName)], y?.AuxiliaryFields?[fstring.NewS(fieldName)]); //, StringComparison.OrdinalIgnoreCase);

                if (result != 0)
                {
                    return sortDirection == KbSortDirection.Descending ? -result : result;
                }
            }
            return 0;
        }
    }
}
