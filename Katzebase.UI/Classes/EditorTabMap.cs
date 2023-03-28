using ICSharpCode.AvalonEdit;

namespace Katzebase.UI.Classes
{
    internal class EditorTabMap
    {
        public ProjectTabPage Tab { get; set; }
        public TextEditor Editor { get; set; }

        public EditorTabMap(ProjectTabPage tab, TextEditor editor)
        {
            Tab = tab;
            Editor = editor;
        }
    }
}
