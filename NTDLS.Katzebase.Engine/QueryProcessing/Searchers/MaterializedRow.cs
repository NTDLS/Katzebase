using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal class MaterializedRow
    {
        public List<string?> Values { get; private set; }

        public KbInsensitiveDictionary<string?> OrderByValues { get; set; } = new();

        public MaterializedRow(List<string?> values)
        {
            Values = values;
        }

        public MaterializedRow()
        {
            Values = new();
        }
    }
}
