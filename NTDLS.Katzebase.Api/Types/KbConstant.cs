using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Api.Types
{
    public class KbConstant
    {
        public string? Value { get; set; }
        public KbBasicDataType DataType { get; set; }

        public KbConstant(string? value, KbBasicDataType dataType)
        {
            Value = value;
            DataType = dataType;
        }
    }
}
