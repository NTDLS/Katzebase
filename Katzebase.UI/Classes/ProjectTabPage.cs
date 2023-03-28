using ICSharpCode.AvalonEdit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Katzebase.UI.Classes
{
    internal class ProjectTabPage : TabPage
    {
        public bool IsSaved { get; set; } = true;
        public TextEditor Editor { get; set; }
        public ProjectTreeNode Node { get; set; }
        public FormFindText FindTextForm { get; set; }
        public FormReplaceText ReplaceTextForm { get; set; }

        public ProjectTabPage(ProjectTreeNode node, TextEditor editor) :
             base(node.Text)
        {
            Node = node;
            Editor = editor;
            FindTextForm = new FormFindText(this);
            ReplaceTextForm = new FormReplaceText(this);
        }

        public bool Save()
        {
            if (Node.NodeType == Constants.ProjectNodeType.Script || Node.NodeType == Constants.ProjectNodeType.Asset
                 || Node.NodeType == Constants.ProjectNodeType.Note)
            {
                File.WriteAllText(Node.FullFilePath, Editor.Text);
                Text = Text.TrimEnd('*');
            }
            else if (Node.NodeType == Constants.ProjectNodeType.Workloads || Node.NodeType == Constants.ProjectNodeType.Workload)
            {
                if (Node.ConfigFilePath == null)
                {
                    throw new Exception("Configuration path should never be null for node type.");
                }
                File.WriteAllText(Node.ConfigFilePath, Editor.Text);
                Text = Text.TrimEnd('*');
            }
            else
            {
                throw new NotImplementedException();
            }

            IsSaved = true;

            return true;
        }
    }
}
