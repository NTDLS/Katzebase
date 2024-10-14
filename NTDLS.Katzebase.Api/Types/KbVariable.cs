using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Api.Types
{
    public class KbVariable
    {
        public string? Value { get; set; }
        public KbBasicDataType DataType { get; set; }

        /// <summary>
        /// Constants are always set, whereas when (IsConstant == false), this means that the variable must be set at execution time.
        /// </summary>
        public bool IsConstant { get; set; }

        public KbVariable(string? value, KbBasicDataType dataType)
        {
            Value = value;
            DataType = dataType;
        }
    }
}
