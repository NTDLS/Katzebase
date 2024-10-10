namespace NTDLS.Katzebase.Api.Payloads.Response
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
