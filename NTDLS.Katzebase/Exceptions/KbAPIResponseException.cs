﻿using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Exceptions
{
    public class KbAPIResponseException : KbExceptionBase
    {
        public KbAPIResponseException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbAPIResponseException(string? message)
            : base($"API exception: {message}.")

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}