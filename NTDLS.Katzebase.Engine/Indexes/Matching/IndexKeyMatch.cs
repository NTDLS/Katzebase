using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Indexes.Matching
{
    public class IndexKeyMatch
    {
        public bool Handled { get; private set; }
        public string Field { get; private set; }
        public string Value { get; private set; }
        public LogicalQualifier LogicalQualifier { get; private set; }

        public void SetHandled()
        {
            Handled = true;
        }

        public IndexKeyMatch(string key, LogicalQualifier logicalQualifier, string value)
        {
            Field = key.ToLower();
            Value = value.ToLower();
            LogicalQualifier = logicalQualifier;
        }
    }
}
