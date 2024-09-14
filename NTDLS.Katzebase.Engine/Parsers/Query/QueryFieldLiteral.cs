using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query
{
    internal class ConditionFieldLiteral
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
