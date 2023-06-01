using Katzebase.Engine.Query;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Indexes
{
    public class IndexKeyMatch
    {
        public bool Handled { get; set; }
        public string Field { get; private set; }
        public string Value { get; private set; }
        public LogicalQualifier LogicalQualifier { get; private set; }

        public IndexKeyMatch(string key, LogicalQualifier logicalQualifier, string value)
        {
            this.Field = key.ToLower();
            this.Value = value.ToLower();
            this.LogicalQualifier = logicalQualifier;
        }
    }
}
