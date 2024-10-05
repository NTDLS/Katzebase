using System.Text.RegularExpressions;

namespace NTDLS.Katzebase.Api
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
    }
}
