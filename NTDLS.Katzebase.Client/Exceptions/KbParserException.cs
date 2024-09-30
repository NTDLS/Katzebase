﻿namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbParserException : KbExceptionBase
    {
        public int? LineNumber { get; set; }

        public KbParserException()
        {
        }

        public KbParserException(int? lineNumber, string message)
            : base(lineNumber == null ? message : $"Syntax error on line {lineNumber:n0}, {message}")
        {
            LineNumber = lineNumber;
        }

        public KbParserException(string message)
            : base(message)
        {
        }
    }
}
