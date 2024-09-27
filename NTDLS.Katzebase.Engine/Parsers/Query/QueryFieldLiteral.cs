using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query
{
    internal class ConditionFieldLiteral<TData> where TData : IStringable
    {
        public TData? Value { get; set; }
        public KbBasicDataType DataType { get; set; }

        public ConditionFieldLiteral(KbBasicDataType dataType, TData? value)
        {
            DataType = dataType;
            Value = value;
        }
    }
}
