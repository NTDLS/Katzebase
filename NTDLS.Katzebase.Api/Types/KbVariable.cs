using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Api.Types
{
    public class KbVariable
    {
        public string? Value { get; set; }
        public KbBasicDataType DataType { get; set; }

        public KbVariable(string? value, KbBasicDataType dataType)
        {
            Value = value;
            DataType = dataType;
        }
    }
}
