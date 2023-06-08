using Katzebase.Engine.Query.Constraints;

namespace Katzebase.Engine.Query
{
    public class UpsertKeyValue
    {
        private string _key = string.Empty;
        public string Key { get { return _key; } set { _key = value.ToLowerInvariant(); } }
        public ConditionValue Value { get; set; } = new ConditionValue();
    }
}
