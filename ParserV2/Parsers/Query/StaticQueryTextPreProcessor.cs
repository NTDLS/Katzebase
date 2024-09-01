using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Types;
using System.Text.RegularExpressions;

namespace ParserV2.Parsers.Query
{
    internal class StaticQueryTextPreProcessor
    {
        public KbInsensitiveDictionary<string> StringLiterals { get; private set; } = new();
        public KbInsensitiveDictionary<string> NumericLiterals { get; private set; } = new();

        public string Text { get; private set; } = string.Empty;

        public static StaticQueryTextPreProcessor Parse(string queryText)
        {
            StaticQueryTextPreProcessor preProcessed = new();

            string text = KbTextUtility.RemoveComments(queryText);

            preProcessed.StringLiterals = SwapOutStringLiterals(ref text);

            //We replace numeric constants and we want to make sure we have 
            //  no numbers next to any conditional operators before we do so.
            text = text.Replace("!=", "$$NotEqual$$");
            text = text.Replace(">=", "$$GreaterOrEqual$$");
            text = text.Replace("<=", "$$LesserOrEqual$$");
            text = text.Replace("(", " ( ");
            text = text.Replace(")", " ) ");
            text = text.Replace(",", " , ");
            text = text.Replace(">", " > ");
            text = text.Replace("<", " < ");
            text = text.Replace("=", " = ");
            text = text.Replace("$$NotEqual$$", " != ");
            text = text.Replace("$$GreaterOrEqual$$", " >= ");
            text = text.Replace("$$LesserOrEqual$$", " <= ");
            text = text.Replace("||", " || ");
            text = text.Replace("&&", " && ");

            preProcessed.NumericLiterals = SwapOutNumericLiterals(ref text);

            int length;
            do
            {
                length = text.Length;
                text = text.Replace("\t", " ");
                text = text.Replace("  ", " ");
            }
            while (length != text.Length);

            text = text.Trim();

            text = text.Replace("(", " ( ").Replace(")", " ) ");

            RemoveComments(ref text);

            TrimAllLines(ref text);
            RemoveEmptyLines(ref text);
            RemoveNewlines(ref text);
            RemoveDoubleWhitespace(ref text);

            preProcessed.Text = text.Trim();

            return preProcessed;
        }

        #region Swap in/out literals.

        /// <summary>
        /// Replaces text literals with tokens to prepare the query for parsing.
        /// </summary>
        public static KbInsensitiveDictionary<string> SwapOutStringLiterals(ref string query)
        {
            var mappings = new KbInsensitiveDictionary<string>();
            var regex = new Regex("\"([^\"\\\\]*(\\\\.[^\"\\\\]*)*)\"|\\'([^\\'\\\\]*(\\\\.[^\\'\\\\]*)*)\\'");
            int literalKey = 0;

            while (true)
            {
                var match = regex.Match(query);

                if (match.Success)
                {
                    string key = $"$s_{literalKey++}$";
                    mappings.Add(key, match.ToString());

                    query = NTDLS.Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                }
                else
                {
                    break;
                }
            }

            return mappings;
        }

        /// <summary>
        /// Replaces numeric literals with tokens to prepare the query for parsing.
        /// </summary>
        public static KbInsensitiveDictionary<string> SwapOutNumericLiterals(ref string query)
        {
            var mappings = new KbInsensitiveDictionary<string>();
            var regex = new Regex(@"(?<=\s|^)(?:\d+\.?\d*|\.\d+)(?=\s|$)");
            int literalKey = 0;

            while (true)
            {
                var match = regex.Match(query);

                if (match.Success)
                {
                    string key = $"$n_{literalKey++}$";
                    mappings.Add(key, match.ToString());

                    query = NTDLS.Helpers.Text.ReplaceRange(query, match.Index, match.Length, key);
                }
                else
                {
                    break;
                }
            }

            return mappings;
        }

        #endregion

        # region Clean query text.

        /// <summary>
        /// Removes all unnecessary whitespace, newlines, comments and replaces literals with tokens to prepare query for parsing.
        /// </summary>
        public void Prepare()
        {

        }

        public static void RemoveComments(ref string query)
        {
            query = "\r\n" + query + "\r\n";

            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"--(.*?)\r?\n";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";

            query = Regex.Replace(query,
                blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                me =>
                {
                    if (me.Value.StartsWith("/*") || me.Value.StartsWith("--"))
                        return me.Value.StartsWith("--") ? Environment.NewLine : "";
                    // Keep the literal strings
                    return me.Value;
                },
                RegexOptions.Singleline);
        }

        public static void RemoveEmptyLines(ref string query)
            => query = Regex.Replace(query, @"^\s+$[\r\n]*", "", RegexOptions.Multiline);

        public static void TrimAllLines(ref string query)
            => query = string.Join("\r\n", query.Split('\n').Select(o => o.Trim()));

        public static void RemoveDoubleWhitespace(ref string query)
            => query = Regex.Replace(query, @"\s+", " ");

        public static void RemoveNewlines(ref string query)
            => query = query.Replace("\r\n", " ");

        #endregion
    }
}
