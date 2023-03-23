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

        public static void EnsureNotNull<T>([NotNull] T? value, [CallerArgumentExpression("value")] string strName = "")
        {
            if (value == null)
            {
                throw new KatzebaseCriticalNull($"{strName} cannot be null.");
            }
        }

        public static void EnsureNotNullOrEmpty([NotNull] Guid? value, [CallerArgumentExpression("value")] string strName = "")
        {
            if (value == null || value == Guid.Empty)
            {
                throw new KatzebaseCriticalNull($"{strName} cannot be null.");
            }
        }

        public static void EnsureNotNullOrEmpty([NotNull] string? value, [CallerArgumentExpression("value")] string strName = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new KatzebaseCriticalNull($"{strName} cannot be null.");
            }
        }

        public static void EnsureNotNullOrWhiteSpace([NotNull] string? value, [CallerArgumentExpression("value")] string strName = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new KatzebaseCriticalNull($"{strName} cannot be null.");
            }
        }
    }
}
