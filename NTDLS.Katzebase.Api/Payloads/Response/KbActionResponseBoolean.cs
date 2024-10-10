namespace NTDLS.Katzebase.Api.Payloads.Response
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
