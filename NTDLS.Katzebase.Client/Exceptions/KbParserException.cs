namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbParserException : KbExceptionBase
    {
        public KbParserException()
        {
        }

        public KbParserException(int? lineNumber, string message)
            : base(lineNumber == null ? message : $"Syntax error on line {lineNumber:n0}, {message}")
        {
        }

        public KbParserException(string message)
            : base(message)
        {
        }
    }
}
