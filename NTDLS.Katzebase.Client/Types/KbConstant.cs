using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Types
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
