namespace Katzebase.PrivateLibrary
{
    public class AmbidextrousConditionValue
    {
        public enum Side
        {
            Left,
            Right
        }

        public string Value { get; set; }
        public Side WhichSide { get; set; }

        public AmbidextrousConditionValue(Side whichSide, string value)
        {
            WhichSide = whichSide;
            Value = value;
        }
    }
}
