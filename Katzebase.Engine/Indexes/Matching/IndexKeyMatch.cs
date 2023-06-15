using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Indexes.Matching
{
    public class IndexKeyMatch
    {
        public bool Handled { get; set; }
        public string Field { get; private set; }
        public string Value { get; private set; }
        public LogicalQualifier LogicalQualifier { get; private set; }

        public IndexKeyMatch(string key, LogicalQualifier logicalQualifier, string value)
        {
            Field = key.ToLower();
            Value = value.ToLower();
            LogicalQualifier = logicalQualifier;
        }
    }
}
