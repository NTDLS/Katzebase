namespace NTDLS.Katzebase.Parsers.Tokens
{
    public class TokenizerLineRange
    {
        public int Line { get; set; }
        public int Start { get; set; }
        public int End { get; set; }

        public TokenizerLineRange(int line, int start, int end)
        {
            Line = line;
            Start = start;
            End = end;
        }
    }
}
