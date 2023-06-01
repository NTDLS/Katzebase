using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class FlatConditionGroup
    {
        public List<ConditionSingle> Conditions = new();

        public LogicalConnector LogicalConnector { get; set; }

        private Guid _sourceSubsetUID;
        public Guid SourceSubsetUID => _sourceSubsetUID;

        public FlatConditionGroup(LogicalConnector logicalConnector, Guid sourceSubsetUID)
        {
            _sourceSubsetUID = sourceSubsetUID;
            LogicalConnector = logicalConnector;
        }
    }
}
