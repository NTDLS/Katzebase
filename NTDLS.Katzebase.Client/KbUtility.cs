using NTDLS.Katzebase.Client.Exceptions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace NTDLS.Katzebase.Client
{
    public static class KbUtility
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

        public static List<string> SplitQueryBatches(string text)
        {
            text = RemoveComments(text);

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

        public static List<string> SplitQueryBatchesOnGO(string text)
        {
            text = RemoveComments(text);

            var lines = text.Replace("\r\n", "\n").Split('\n').Where(o => string.IsNullOrWhiteSpace(o) == false).ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = lines[i].Trim();
            }

            var batches = new List<string>();

            var batchText = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.ToLower() == "go")
                {
                    if (batchText.Length > 0)
                    {
                        batches.Add(batchText.ToString());
                        batchText.Clear();
                    }
                }
                else
                {
                    batchText.AppendLine(line.Trim());
                }
            }

            if (batchText.Length > 0)
            {
                batches.Add(batchText.ToString());
                batchText.Clear();
            }

            return batches;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            return (new StackTrace())?.GetFrame(1)?.GetMethod()?.Name ?? "{unknown frame}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureNotNull<T>([NotNull] T? value, string? message = null, [CallerArgumentExpression(nameof(value))] string strName = "")
        {
            if (value == null)
            {
                if (message == null)
                {
                    throw new KbNullException($"Value should not be null: '{strName}'.");
                }
                else
                {
                    throw new KbNullException(message);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureNotNullOrEmpty([NotNull] Guid? value, [CallerArgumentExpression(nameof(value))] string strName = "")
        {
            if (value == null || value == Guid.Empty)
            {
                throw new KbNullException($"Value should not be null or empty: '{strName}'.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureNotNullOrEmpty([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string strName = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new KbNullException($"Value should not be null or empty: '{strName}'.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureNotNullOrWhiteSpace([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string strName = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new KbNullException($"Value should not be null or empty: '{strName}'.");
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
