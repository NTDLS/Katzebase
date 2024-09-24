using static NTDLS.Katzebase.Client.KbConstants;
using fs;

namespace NTDLS.Katzebase.Engine.Parsers.Query
{
    internal class ConditionFieldLiteral
    {
        public fstring? Value { get; set; }
        public KbBasicDataType DataType { get; set; }

        public ConditionFieldLiteral(KbBasicDataType dataType, fstring? value)
        {
            DataType = dataType;
            Value = value;
        }
    }
}
