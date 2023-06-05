namespace Katzebase.UI.Classes
{
    internal static class FormUtility
    {
        public static TreeNode? FindNode(TreeNode root, string text)
        {
            foreach (var node in root.Nodes.Cast<TreeNode>())
            {
                if (node.Text == text)
                {
                    return node;
                }
            }
            return null;
        }

        public static int SortChildNodes(TreeNode node)
        {
            int moves = 0;

            for (int i = 0; i < node.Nodes.Count - 1; i++)
            {
                if (node.Nodes[i].Text.CompareTo(node.Nodes[i + 1].Text) > 0)
                {
                    int nodeIndex = node.Nodes[i].Index;
                    var nodeCopy = node.Nodes[i].Clone() as TreeNode;
                    node.Nodes.Remove(node.Nodes[i]);

                    node.Nodes.Insert(nodeIndex + 1, nodeCopy);
                    moves++;
                }
                else if (node.Nodes[i + 1].Text.CompareTo(node.Nodes[i].Text) < 0)
                {
                    int nodeIndex = node.Nodes[i].Index;
                    var nodeCopy = node.Nodes[i].Clone() as TreeNode;
                    node.Nodes.Remove(node.Nodes[i]);

                    node.Nodes.Insert(nodeIndex - 1, nodeCopy);
                    moves++;
                }
            }

            if (moves > 0)
            {
                return SortChildNodes(node);
            }

            return moves;
        }

        public static Image TransparentImage(Image image)
        {
            Bitmap toolBitmap = new(image);
            toolBitmap.MakeTransparent(Color.Magenta);
            return toolBitmap;
        }

        public static Image ImageFromBytes(byte[] imageBytes)
        {
            using (var ms = new MemoryStream(imageBytes))
            {
                return Image.FromStream(ms);
            }
        }

        #region Tree node factories.

        public static ProjectTreeNode CreateSchemaNode(string name)
        {
            var node = new ProjectTreeNode(name)
            {
                NodeType = Constants.ProjectNodeType.Schema,
                ImageKey = "Schema",
                SelectedImageKey = "Schema"
            };

            return node;
        }

        public static ProjectTreeNode CreateIndexNode(string name)
        {
            var node = new ProjectTreeNode(name)
            {
                NodeType = Constants.ProjectNodeType.Index,
                ImageKey = "Index",
                SelectedImageKey = "Index"
            };

            return node;
        }


        #endregion
    }
}
