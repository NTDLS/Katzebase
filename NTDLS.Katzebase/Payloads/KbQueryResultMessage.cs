using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Payloads
{
    public class KbQueryResultMessage
    {
        public KbMessageType MessageType { get; set; }
        public string Text { get; set; }

        public KbQueryResultMessage(string text, KbMessageType type)
        {
            MessageType = type;
            Text = text;
        }
    }
}
