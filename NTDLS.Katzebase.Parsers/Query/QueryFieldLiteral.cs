using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Parsers.Query
{
    public class QueryFieldLiteral(KbBasicDataType dataType, string? value)
    {
        public string? Value { get; set; } = value;
        public KbBasicDataType DataType { get; set; } = dataType;
    }
}
