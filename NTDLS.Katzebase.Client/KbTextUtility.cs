using System.Text;
using System.Text.RegularExpressions;

namespace NTDLS.Katzebase.Client
{
    public static class KbTextUtility
    {
        /// <summary>
        /// Removes comments while remaining code line positions, replaces "\r\n" with "\n" and adds a trailing "\n",
        /// </summary>
        public static string RemoveNonCode(string input)
        {
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"--(.*?)\r?\n";
            var strings = @"'((\\[^\n]|[^'\n])*)'";
            var verbatimStrings = @"@('[^']*')+";

            string noComments = Regex.Replace(input,
                blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                me =>
                {
                    if (me.Value.StartsWith("/*"))
                    {
                        var numberOfNewlines = me.Value.Count(c => c == '\n');
                        return string.Concat(Enumerable.Repeat(Environment.NewLine, numberOfNewlines));
                    }
                    else if (me.Value.StartsWith("--"))
                    {
                        return Environment.NewLine;
                    }
                    return me.Value;
                },
                RegexOptions.Singleline);

            return noComments.TrimEnd().Replace("\r\n", "\n") + "\n";
        }

        /// <summary>
        /// Splits query text on "GO", removes comments while remaining code line positions, replaces "\r\n" with "\n" and adds a trailing "\n",
        /// </summary>
        public static List<string> SplitQueryBatchesOnGO(string text)
        {
            text = RemoveNonCode(text);

            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();

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
    }
}
