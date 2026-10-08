using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace NTDLS.Katzebase.Api.Exceptions
{
    /// <summary>
    /// Converts exceptions raised while making a request to the server into the Katzebase exception types that callers
    /// can catch: the exception type that the server threw (see <see cref="KbExceptionBase.Source"/>), a
    /// <see cref="KbTimeoutException"/>, a <see cref="KbConnectionException"/> or, for anything else, a
    /// <see cref="KbAPIResponseException"/>. The original exception is preserved where one is available.
    /// </summary>
    internal static class KbRemoteExceptionMapper
    {
        private static readonly ConcurrentDictionary<string, Type?> _exceptionTypes = new(StringComparer.Ordinal);

        public static Exception Map(Exception exception, bool isConnected)
        {
            //Unwrap task and reflection wrappers.
            while ((exception is AggregateException aggregate && aggregate.InnerExceptions.Count == 1)
                || (exception is TargetInvocationException && exception.InnerException != null))
            {
                exception = exception.InnerException!;
            }

            if (exception is KbExceptionBase || exception is OperationCanceledException)
            {
                return exception; //Already meaningful to the caller (or a cancellation the caller requested).
            }

            var remoteException = FromSource(exception);
            if (remoteException != null)
            {
                return remoteException;
            }

            if (exception is TimeoutException || exception.Message.Contains("timeout", StringComparison.InvariantCultureIgnoreCase))
            {
                return new KbTimeoutException($"The server did not reply within the query timeout: {exception.Message}");
            }

            if (isConnected == false)
            {
                return new KbConnectionException($"The connection to the server was lost: {exception.Message}", exception);
            }

            return new KbAPIResponseException(exception.Message, exception);
        }

        /// <summary>
        /// Recreates the exception that the server threw from the "Katzebase:[type name][:detail]" source it carries.
        /// </summary>
        private static Exception? FromSource(Exception exception)
        {
            var source = exception.Source;
            if (source == null || source.StartsWith(KbExceptionBase.SourcePrefix, StringComparison.Ordinal) == false)
            {
                return null;
            }

            var parts = source[KbExceptionBase.SourcePrefix.Length..].Split(':', 2);
            var typeName = parts[0];
            var detail = parts.Length > 1 ? parts[1] : null;

            var type = _exceptionTypes.GetOrAdd(typeName, name =>
                typeof(KbExceptionBase).Assembly.GetTypes().FirstOrDefault(o =>
                    o.Name == name && typeof(KbExceptionBase).IsAssignableFrom(o) && o.IsAbstract == false));

            if (type == null)
            {
                return null;
            }

            if (type == typeof(KbParserException))
            {
                int? lineNumber = int.TryParse(detail, NumberStyles.Integer, CultureInfo.InvariantCulture, out var line) ? line : null;
                return new KbParserException(lineNumber, exception.Message);
            }

            //Every Katzebase exception has a (string message) constructor.
            var constructor = type.GetConstructor([typeof(string)]);
            return constructor?.Invoke([exception.Message]) as Exception;
        }
    }
}
