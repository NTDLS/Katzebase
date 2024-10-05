using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Parsers.Query
{
    public class ConditionFieldLiteral
    {
        public string? Value { get; set; }
        public KbBasicDataType DataType { get; set; }

        public ConditionFieldLiteral(KbBasicDataType dataType, string? value)
        {
            DataType = dataType;
            Value = value;
        }
    }
}
