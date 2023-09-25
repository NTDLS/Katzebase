﻿using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Exceptions
{
    public class KbDeadlockException : KbExceptionBase
    {
        public KbDeadlockException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbDeadlockException(string message)
            : base($"Deadlock exception: {message}.")

        {
            Severity = KbLogSeverity.Warning;
        }
    }
}