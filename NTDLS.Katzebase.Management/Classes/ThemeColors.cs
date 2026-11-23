using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using NTDLS.WinFormsHelpers;
using System.Xml;
using WpfMedia = System.Windows.Media;

namespace NTDLS.Katzebase.Management.Classes
{
    /// <summary>
    /// Colors for the things the WinForms theme does not paint: the AvalonEdit editors (WPF).
    /// Whether dark mode is on comes from <see cref="Theming.IsDarkMode"/>.
    /// </summary>
    internal static class ThemeColors
    {
        public static Color EditorBackground => Theming.IsDarkMode ? Color.FromArgb(0x1E, 0x1E, 0x1E) : Color.White;
        public static Color EditorForeground => Theming.IsDarkMode ? Color.FromArgb(0xDC, 0xDC, 0xDC) : Color.Black;
        public static Color EditorLineNumbers => Theming.IsDarkMode ? Color.Gray : Color.DimGray;

        private static WpfMedia.SolidColorBrush Brush(Color color)
            => new(WpfMedia.Color.FromRgb(color.R, color.G, color.B));

        /// <summary>
        /// Gives an editor the colors of the current theme. (The editor is a WPF control, so the WinForms theme does not reach it.)
        /// </summary>
        public static void ApplyTheme(TextEditor editor)
        {
            editor.Background = Brush(EditorBackground);
            editor.Foreground = Brush(EditorForeground);
            editor.LineNumbersForeground = Brush(EditorLineNumbers);

            if (Theming.IsDarkMode)
            {
                editor.TextArea.SelectionBrush = new WpfMedia.SolidColorBrush(WpfMedia.Color.FromArgb(0x80, 0x26, 0x4F, 0x78));
                editor.TextArea.SelectionForeground = null;
                editor.TextArea.SelectionBorder = null;
                editor.TextArea.Caret.CaretBrush = Brush(EditorForeground);
            }
        }

        /// <summary>
        /// Brings a color up to a brightness that can be read on a dark background, keeping its hue.
        /// </summary>
        public static Color Lighten(Color color)
        {
            if (color.GetBrightness() >= 0.6f)
            {
                return color;
            }

            float hue = color.GetHue(), saturation = Math.Max(color.GetSaturation(), 0.5f), lightness = 0.68f;
            float c = (1 - Math.Abs((2 * lightness) - 1)) * saturation;
            float x = c * (1 - Math.Abs(((hue / 60f) % 2) - 1));
            float m = lightness - (c / 2);

            (float r, float g, float b) = (int)(hue / 60f) switch
            {
                0 => (c, x, 0f),
                1 => (x, c, 0f),
                2 => (0f, c, x),
                3 => (0f, x, c),
                4 => (x, 0f, c),
                _ => (c, 0f, x)
            };

            return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
        }

        /// <summary>
        /// The colors of a named highlighting color when on a dark background. Colors not listed are lightened.
        /// </summary>
        private static readonly Dictionary<string, WpfMedia.Color> DarkPalette = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Punctuation"] = WpfMedia.Color.FromRgb(0xDC, 0xDC, 0xDC),
            ["NumberLiteral"] = WpfMedia.Color.FromRgb(0xB5, 0xCE, 0xA8),
            ["KbMarkup"] = WpfMedia.Color.FromRgb(0xDC, 0xDC, 0xAA),
            ["KbConfig"] = WpfMedia.Color.FromRgb(0x4E, 0xC9, 0xB0),
            ["KbFunction"] = WpfMedia.Color.FromRgb(0xDC, 0x8C, 0xE6),
            ["KbConst"] = WpfMedia.Color.FromRgb(0xC5, 0x86, 0xC0),
            ["Comment"] = WpfMedia.Color.FromRgb(0x6A, 0x99, 0x55),
            ["Char"] = WpfMedia.Color.FromRgb(0xD6, 0x9D, 0x85),
            ["Keywords"] = WpfMedia.Color.FromRgb(0x56, 0x9C, 0xD6),
        };

        /// <summary>
        /// Loads the Katzebase syntax definition (.xshd) from the resources, with colors readable on a dark background in dark mode.
        /// </summary>
        public static IHighlightingDefinition LoadHighlighting()
        {
            using var stringReader = new StringReader(Properties.Resources.Highlighter);
            using var reader = XmlReader.Create(stringReader);
            var xshd = HighlightingLoader.LoadXshd(reader);

            if (Theming.IsDarkMode)
            {
                foreach (var color in xshd.Elements.OfType<XshdColor>())
                {
                    if (color.Name != null && DarkPalette.TryGetValue(color.Name, out var onDark))
                    {
                        color.Foreground = new SimpleHighlightingBrush(onDark);
                    }
                    else if (color.Foreground is SimpleHighlightingBrush brush && brush.GetBrush(null) is WpfMedia.SolidColorBrush solid)
                    {
                        var lightened = Lighten(Color.FromArgb(solid.Color.R, solid.Color.G, solid.Color.B));
                        color.Foreground = new SimpleHighlightingBrush(WpfMedia.Color.FromRgb(lightened.R, lightened.G, lightened.B));
                    }
                }
            }

            return HighlightingLoader.Load(xshd, HighlightingManager.Instance);
        }
    }
}
