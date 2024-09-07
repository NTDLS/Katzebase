using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query
{
    internal class QueryFieldLiteral
    {
        public string? Value { get; set; }
        public KbBasicDataType DataType { get; set; }

        public QueryFieldLiteral(KbBasicDataType dataType, string? value)
        {
            DataType = dataType;
            Value = value;
        }
    }
}
