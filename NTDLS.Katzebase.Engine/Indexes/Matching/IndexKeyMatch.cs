using static NTDLS.Katzebase.Engine.Library.EngineConstants;
using static NTDLS.Katzebase.Parsers.Constants;

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
            Field = key.ToLowerInvariant();
            Value = value.ToLowerInvariant();
            LogicalQualifier = logicalQualifier;
        }
    }
}
