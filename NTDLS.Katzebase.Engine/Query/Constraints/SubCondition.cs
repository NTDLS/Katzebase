using NTDLS.Katzebase.Engine.Indexes.Matching;

namespace NTDLS.Katzebase.Engine.Query.Constraints
{
    internal class SubCondition
    {
        public bool IsRoot { get; set; } = false;
        public string SubConditionKey { get; set; }
        public string Condition { get; set; }
        public List<Condition> Conditions { get; set; } = new();
        public HashSet<string> SubConditionKeys { get; set; } = new();
        public HashSet<string> ConditionKeys { get; set; } = new();

        /// <summary>
        /// If this condition is covered by an index, this is the index which we will use.
        /// </summary>
        public IndexSelection? IndexSelection { get; set; }

        public SubCondition(string subConditionKey, string condition)
        {
            SubConditionKey = subConditionKey;
            Condition = condition;
        }
    }
}
