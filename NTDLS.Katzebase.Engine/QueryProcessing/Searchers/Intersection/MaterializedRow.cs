using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class MaterializedRow
    {
        public List<string?> Values { get; private set; }
        public KbInsensitiveDictionary<string?> OrderByValues { get; private set; } = new();

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
