using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Sorting
{
    internal class MaterializedRowEqualityComparer : IEqualityComparer<MaterializedRow>
    {
        public bool Equals(MaterializedRow? x, MaterializedRow? y)
        {
            if (x == null || y == null)
                return false;

            // Compare values list case-insensitively, order matters.
            return x.Values.SequenceEqual(y.Values, StringComparer.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(MaterializedRow obj)
        {
            return obj.Values
                .Select(value => value?.ToLowerInvariant().GetHashCode() ?? 0)
                .Aggregate(0, (hash, valueHash) => hash ^ valueHash);
        }
    }
}
