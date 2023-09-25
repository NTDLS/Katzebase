﻿using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Exceptions
{
    public class KbObjectAlreadysExistsException : KbExceptionBase
    {
        public KbObjectAlreadysExistsException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbObjectAlreadysExistsException(string? message)
            : base($"Object already exists exception: {message}.")

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}