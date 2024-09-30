namespace NTDLS.Katzebase.Parsers.Tokens
{
    public class TokenizerLineRange
    {
        public int Line { get; set; }
        public int Begin { get; set; }
        public int End { get; set; }

        public TokenizerLineRange(int line, int start, int end)
        {
            Line = line;
            Begin = start;
            End = end;
        }
    }
}
