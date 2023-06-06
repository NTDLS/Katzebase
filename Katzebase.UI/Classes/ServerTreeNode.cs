using static Katzebase.UI.Classes.Constants;

namespace Katzebase.UI.Classes
{
    public class ServerTreeNode : TreeNode
    {
        public ServerNodeType NodeType { get; set; }

        public string ServerAddress { get; set; } = string.Empty;

        public ServerTreeNode(string name) :
             base(name)
        {
        }
    }
}
