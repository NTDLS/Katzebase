using Katzebase.PublicLibrary.Exceptions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Katzebase.PublicLibrary
{
    public static class Utility
    {
        public static string RemoveComments(string input)
        {
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"--(.*?)\r?\n";
            var strings = @"'((\\[^\n]|[^'\n])*)'";
            var verbatimStrings = @"@('[^']*')+";

            string noComments = Regex.Replace(input,
                blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                me =>
                {
                    if (me.Value.StartsWith("/*") || me.Value.StartsWith("--"))
                        return me.Value.StartsWith("--") ? Environment.NewLine : "";
                    // Keep the literal strings
                    return me.Value;
                },
                RegexOptions.Singleline);

            return noComments;
        }

        public static List<string> SplitQueryTextIntoBatches(string text)
        {
            text = Utility.RemoveComments(text);

            var lines = text.Replace("\r\n", "\n").Split('\n').Where(o => string.IsNullOrWhiteSpace(o) == false).ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = lines[i].Trim();
            }

            text = string.Join("\r\n", lines).Trim() + "\r\n";

            var batches = text.Split(";\r\n", StringSplitOptions.RemoveEmptyEntries)
                .Where(o => string.IsNullOrWhiteSpace(o) == false).ToList();

            return batches;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            return (new StackTrace())?.GetFrame(1)?.GetMethod()?.Name ?? "{unknown frame}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureNotNullOrEmpty([NotNull] Guid? value, [CallerArgumentExpression("value")] string strName = "")
        {
            if (value == null || value == Guid.Empty)
            {
                throw new KbNullException($"Value should not be null or empty {nameof(strName)}.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureNotNullOrEmpty([NotNull] string? value, [CallerArgumentExpression("value")] string strName = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new KbNullException($"Value should not be null or empty {nameof(strName)}.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool condition, string message)
        {
            if (condition)
            {
                throw new KbAssertException(message);
            }
        }
    }
}
