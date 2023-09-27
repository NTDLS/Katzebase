namespace NTDLS.Katzebase.Client.Payloads
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
