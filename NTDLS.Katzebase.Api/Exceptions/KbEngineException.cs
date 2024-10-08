﻿namespace NTDLS.Katzebase.Api.Exceptions
{
    /// <summary>
    /// Used to report unexpected engine operations. Not quite fatal, but need to be reported.
    /// </summary>
    public class KbEngineException : KbExceptionBase
    {
        public KbEngineException()
        {
            Severity = KbConstants.KbLogSeverity.Error;
        }

        public KbEngineException(string message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Error;
        }
    }
}
