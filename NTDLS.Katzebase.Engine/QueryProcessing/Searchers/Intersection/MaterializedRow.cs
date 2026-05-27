using NTDLS.Katzebase.Api.Types;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class MaterializedRow
    {
        public List<string?> Values { get; private set; }

        /// <summary>
        /// Raw string values for each ORDER BY field, keyed by field alias.
        /// Used as the fallback when a field cannot be parsed as a number.
        /// </summary>
        public KbInsensitiveDictionary<string?> OrderByValues { get; private set; } = new();

        /// <summary>
        /// Pre-parsed numeric values for each ORDER BY field, keyed by field alias.
        /// Populated alongside <see cref="OrderByValues"/> at result-build time so the
        /// sort comparer can do a single numeric compare instead of calling TryParse
        /// on every comparison during the O(n log n) sort.  Null when the field value
        /// is not numeric.
        /// </summary>
        public KbInsensitiveDictionary<double?> OrderByNumericValues { get; private set; } = new();

        public MaterializedRow(List<string?> values)
        {
            Values = values;
        }

        public MaterializedRow(List<string?> values, KbInsensitiveDictionary<string?> orderByValues)
        {
            Values = values;
            OrderByValues = orderByValues;
        }

        public MaterializedRow()
        {
            Values = new();
        }
    }
}
