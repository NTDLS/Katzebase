namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbActionResponseInteger : KbBaseActionResponse
    {
        public int Value { get; set; }

        public KbActionResponseInteger(int value)
        {
            Value = value;
        }

        public KbActionResponseInteger()
        {
        }
    }
}
