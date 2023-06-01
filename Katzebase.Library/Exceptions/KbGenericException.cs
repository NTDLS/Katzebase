using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KbGenericException : KbExceptionBase
    {
        public KbGenericException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbGenericException(string ?message)
            : base(message)

        {
            Severity = LogSeverity.Exception;
        }
    }
}