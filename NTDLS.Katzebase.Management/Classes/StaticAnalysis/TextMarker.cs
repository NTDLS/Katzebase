namespace NTDLS.Katzebase.Management.Classes.StaticAnalysis
{
    public class TextMarker : ITextMarker
    {
        public TextMarker(int startOffset, int length, string toolTip, System.Windows.Media.Color? squigglyLineColor)
        {
            StartOffset = startOffset;
            Length = length;
            ToolTip = toolTip;
            SquigglyLineColor = squigglyLineColor;
        }

        public int StartOffset { get; }
        public int Length { get; }
        public string ToolTip { get; set; }
        public Color? ForegroundColor { get; set; }
        public Color? BackgroundColor { get; set; }
        public System.Windows.Media.Color? SquigglyLineColor { get; set; }

        public void Delete()
        {
            // Handle marker deletion if necessary
        }
    }
}
