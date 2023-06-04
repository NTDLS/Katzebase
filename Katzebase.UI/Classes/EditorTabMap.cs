using ICSharpCode.AvalonEdit;

namespace Katzebase.UI.Classes
{
    internal class EditorTabMap
    {
        public TabFilePage Tab { get; set; }
        public TextEditor Editor { get; set; }

        public EditorTabMap(TabFilePage tab, TextEditor editor)
        {
            Tab = tab;
            Editor = editor;
        }
    }
}
