namespace NTDLS.Katzebase.Management.Controls
{
    public class DoubleBufferedTreeView : TreeView
    {
        public DoubleBufferedTreeView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }
}
