namespace NTDLS.Katzebase.Parsers.Tokens
{
    public class TokenizerLineRange(int line, int start, int end)
    {
        public int Line { get; set; } = line;
        public int Begin { get; set; } = start;
        public int End { get; set; } = end;
    }
}
