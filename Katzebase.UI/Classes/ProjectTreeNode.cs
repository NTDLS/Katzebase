using static Katzebase.UI.Classes.Constants;

namespace Katzebase.UI.Classes
{
    public class ProjectTreeNode : TreeNode
    {
        public ProjectNodeType NodeType { get; set; }

        public ProjectTreeNode(string name) :
             base(name)
        {
        }
    }
}
