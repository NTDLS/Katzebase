namespace NTDLS.Katzebase.Api.Payloads.Response
{
    public class KbActionResponseUInt : KbBaseActionResponse
    {
        public uint Value { get; set; }

        public KbActionResponseUInt(uint value)
        {
            Value = value;
        }

        public KbActionResponseUInt()
        {
        }
    }
}
