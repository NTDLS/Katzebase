namespace NTDLS.Katzebase.Api.Exceptions
{
    public class KbParserException : KbExceptionBase
    {
        public int? LineNumber { get; set; }

        protected override string? TransportDetail
            => LineNumber?.ToString(System.Globalization.CultureInfo.InvariantCulture);

        public KbParserException()
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }

        public KbParserException(int? lineNumber, string message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
            LineNumber = lineNumber;
        }

        public KbParserException(string message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }
    }
}
