using ICSharpCode.AvalonEdit;

namespace Katzebase.UI.Classes
{
    internal class TabInfo
    {
        public TabFile TabFile { get; set; }
        public TextEditor Editor { get; set; }
        public TabFilePage Tab { get; set; }

        public TabInfo(TabFile tabFile, TextEditor editor, TabFilePage tab)
        {
            TabFile = tabFile;
            Editor = editor;
            Tab = tab;
        }
    }
}
