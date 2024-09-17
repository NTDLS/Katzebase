using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
{
    internal interface ICondition
    {
        public LogicalConnector Connector { get; }
    }
}
