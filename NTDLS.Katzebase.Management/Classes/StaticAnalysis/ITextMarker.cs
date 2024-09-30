namespace NTDLS.Katzebase.Management.Classes.StaticAnalysis
{
    public interface ITextMarker
    {
        int StartOffset { get; }
        int Length { get; }
        string ToolTip { get; set; }
        Color? ForegroundColor { get; set; }
        Color? BackgroundColor { get; set; }
        System.Windows.Media.Color? SquigglyLineColor { get; set; }
        void Delete();
    }
}
