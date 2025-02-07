using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Api.Models
{
    public class KbQueryResultMessage
    {
        public KbMessageType MessageType { get; set; }
        public string Text { get; set; }

        public KbQueryResultMessage(string text, KbMessageType messageType)
        {
            MessageType = messageType;
            Text = text;
        }
    }
}
