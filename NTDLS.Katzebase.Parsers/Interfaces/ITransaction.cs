using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Parsers.Interfaces
{
    public interface ITransaction
    {
        void AddWarning(KbTransactionWarning warning, string message = "");
        void AddMessage(string text, KbMessageType type);
    }
}
