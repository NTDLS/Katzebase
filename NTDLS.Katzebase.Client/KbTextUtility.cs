using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace NTDLS.Katzebase.Client
{
    public static class KbTextUtility
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
            => (new StackTrace())?.GetFrame(1)?.GetMethod()?.Name ?? "{unknown frame}";
    }
}
