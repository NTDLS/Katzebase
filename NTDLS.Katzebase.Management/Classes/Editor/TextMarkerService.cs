using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;
using System.Windows.Threading;

namespace NTDLS.Katzebase.Management.Classes.Editor
{
    public class TextMarkerService : IBackgroundRenderer
    {
        private readonly TextEditor _editor;
        private readonly List<SyntaxErrorTextMarker> _markers = new();
        private readonly System.Windows.Controls.ToolTip _toolTip = new();
        private readonly DispatcherTimer _tooltipTimer = new();

        // Property to specify the rendering layer.
        public KnownLayer Layer => KnownLayer.Selection;

        public TextMarkerService(TextEditor editor)
        {
            _editor = editor;
            _editor.TextArea.TextView.BackgroundRenderers.Add(this);

            _editor.TextArea.MouseMove += TextArea_MouseMove;
            _editor.TextArea.MouseLeave += TextArea_MouseLeave;

            _tooltipTimer.Interval = TimeSpan.FromMilliseconds(500);
            _tooltipTimer.Tick += _tooltipTimer_Tick;
        }

        private void _tooltipTimer_Tick(object? sender, EventArgs e)
        {
            _tooltipTimer.Stop();
            _toolTip.IsOpen = true;
        }

        public SyntaxErrorTextMarker Create(int startOffset, int length, string toolTip, System.Windows.Media.Color? squigglyLineColor)
        {
            var marker = new SyntaxErrorTextMarker(startOffset, length, toolTip, squigglyLineColor);
            _markers.Add(marker);
            return marker;
        }

        private void TextArea_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                var position = _editor.GetPositionFromPoint(e.GetPosition(_editor));
                if (position.HasValue)
                {
                    var offset = _editor.Document.GetOffset(position.Value.Line, position.Value.Column);
                    var marker = _markers.FirstOrDefault(m => m.StartOffset <= offset && m.StartOffset + m.Length >= offset);

                    _tooltipTimer.Stop();

                    if (marker != null && !string.IsNullOrEmpty(marker.ToolTip))
                    {
                        _toolTip.Content = Helpers.Text.SoftWrap(marker.ToolTip, 50, [',', ' ', '\t', '.']);
                        _toolTip.PlacementTarget = _editor;
                        _tooltipTimer.Start();
                    }
                    else
                    {
                        _toolTip.IsOpen = false; // Hide tooltip if no marker
                        _tooltipTimer.Stop();
                    }
                }
            }
            catch { }
        }

        private void TextArea_MouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            _toolTip.IsOpen = false; // Hide tooltip when mouse leaves the text area
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            try
            {
                foreach (var marker in _markers)
                {
                    foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView,
                        new TextSegment { StartOffset = marker.StartOffset, EndOffset = marker.StartOffset + marker.Length }))
                    {
                        if (marker.SquigglyLineColor.HasValue)
                        {
                            var pen = new System.Windows.Media.Pen(new SolidColorBrush(marker.SquigglyLineColor.Value), 1);
                            pen.Freeze();

                            double yOffset = rect.Bottom + 2; // Adjust for squiggly line position
                            drawingContext.DrawGeometry(null, pen, CreateSquigglyGeometry(rect.Left, rect.Right, yOffset));
                        }
                    }
                }
            }
            catch { }
        }

        private StreamGeometry CreateSquigglyGeometry(double startX, double endX, double yOffset)
        {
            var geometry = new StreamGeometry();

            using (var context = geometry.Open())
            {
                context.BeginFigure(new System.Windows.Point(startX, yOffset), false, false);

                for (double x = startX; x < endX; x += 4)
                {
                    context.LineTo(new System.Windows.Point(x + 2, yOffset - 2), true, false);
                    context.LineTo(new System.Windows.Point(x + 4, yOffset), true, false);
                }
            }

            geometry.Freeze();
            return geometry;
        }

        public void ClearMarkers()
        {
            try
            {
                _markers.Clear();
            }
            catch { }
        }
    }
}
