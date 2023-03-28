using ICSharpCode.AvalonEdit;

namespace Katzebase.UI.Classes
{
    internal class TabInfo
    {
        public ProjectTreeNode Node { get; set; }
        public TextEditor Editor { get; set; }
        public ProjectTabPage Tab { get; set; }

        public TabInfo(ProjectTreeNode node, TextEditor editor, ProjectTabPage tab)
        {
            Node = node;
            Editor = editor;
            Tab = tab;
        }
    }
}
