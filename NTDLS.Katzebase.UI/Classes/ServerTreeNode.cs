using static NTDLS.Katzebase.UI.Classes.Constants;

namespace NTDLS.Katzebase.UI.Classes
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
