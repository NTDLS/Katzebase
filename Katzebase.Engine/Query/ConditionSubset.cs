using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class ConditionSubset : ConditionBase
    {
        public ConditionGroup Group { get; set; } = new();
    }
}
