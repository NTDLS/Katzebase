namespace Katzebase.PrivateLibrary
{
    public class AmbidextrousConditionValue
    {
        public enum WhichSide
        {
            None,
            Left,
            Right
        }

        public string Value { get; set; }
        public WhichSide Side { get; set; }

        public AmbidextrousConditionValue(WhichSide side, string value)
        {
            Side = side;
            Value = value;
        }
    }
}
