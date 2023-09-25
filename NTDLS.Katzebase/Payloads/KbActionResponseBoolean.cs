namespace NTDLS.Katzebase.Payloads
{
    public class KbActionResponseBoolean : KbBaseActionResponse
    {
        public bool Value { get; set; }

        public KbActionResponseBoolean(bool value)
        {
            Value = value;
        }

        public KbActionResponseBoolean()
        {
        }
    }
}
