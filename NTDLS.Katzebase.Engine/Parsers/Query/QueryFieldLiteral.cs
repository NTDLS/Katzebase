using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query
{
    public class QueryFieldLiteral
    {
        public string Value { get; set; }
        public BasicDataType DataType { get; set; }

        public QueryFieldLiteral(BasicDataType dataType, string value)
        {
            DataType = dataType;
            Value = value;
        }
    }
}
