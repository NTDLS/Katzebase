using Katzebase.Library.Exceptions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Katzebase.Library
{
    public static class Utility
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            return (new StackTrace())?.GetFrame(1)?.GetMethod()?.Name ?? "{unknown frame}";
        }

        public static void EnsureNotNull<T>([NotNull] T? value, string? message = null, [CallerArgumentExpression("value")] string strName = "")
        {
            if (value == null)
            {
                if (message == null)
                {
                    throw new KbNullException($"Value should not be null {nameof(strName)}.");
                }
                else
                {
                    throw new KbNullException(message);
                }
            }
        }

        public static void EnsureNotNullOrEmpty([NotNull] Guid? value, [CallerArgumentExpression("value")] string strName = "")
        {
            if (value == null || value == Guid.Empty)
            {
                throw new KbNullException($"Value should not be null or empty {nameof(strName)}.");
            }
        }

        public static void EnsureNotNullOrEmpty([NotNull] string? value, [CallerArgumentExpression("value")] string strName = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new KbNullException($"Value should not be null or empty {nameof(strName)}.");
            }
        }

        public static void EnsureNotNullOrWhiteSpace([NotNull] string? value, [CallerArgumentExpression("value")] string strName = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new KbNullException($"Value should not be null or empty {nameof(strName)}.");
            }
        }

        [Conditional("DEBUG")]
        public static void AssertIfDebug(bool condition, string message)
        {
            if (condition)
            {
                throw new KbAssertException(message);
            }
        }
    }
}
