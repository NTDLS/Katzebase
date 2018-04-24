using System;
using static Dokdex.Engine.Constants;

namespace Dokdex.Engine.Exceptions
{
    public class DokdexExceptionBase : Exception
    {
        public LogSeverity Severity { get; set; }

        public DokdexExceptionBase()
        {
            Severity = LogSeverity.Exception;
        }

        public DokdexExceptionBase(string message)
            : base(message)

        {
            Severity = LogSeverity.Exception;
        }
    }
}
