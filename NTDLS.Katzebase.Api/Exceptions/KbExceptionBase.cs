using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Api.Exceptions
{
    public class KbExceptionBase : Exception
    {
        /// <summary>
        /// Prefix of the Source of every Katzebase exception: "Katzebase:[exception type name]".
        /// </summary>
        internal const string SourcePrefix = "Katzebase:";

        public KbLogSeverity Severity { get; set; } = KbLogSeverity.Debug;

        public KbExceptionBase()
        {
        }

        public KbExceptionBase(string? message)
            : base(message)
        {
        }

        public KbExceptionBase(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Identifies the exception type (and any details needed to recreate it, see <see cref="TransportDetail"/>).
        ///
        /// Only an exception's message and source are returned to the client when the server fails to process a request,
        /// so the source is used to carry the exception's type. This lets the client rethrow the same exception type
        /// that the server threw (see KbRemoteExceptionMapper), so callers can catch e.g. KbDuplicateKeyViolationException.
        /// </summary>
        public override string? Source
        {
            get => TransportDetail == null ? $"{SourcePrefix}{GetType().Name}" : $"{SourcePrefix}{GetType().Name}:{TransportDetail}";
            set { }
        }

        /// <summary>
        /// Additional information that is carried to the client in <see cref="Source"/>, e.g. a parser error's line number.
        /// </summary>
        protected virtual string? TransportDetail => null;
    }
}
