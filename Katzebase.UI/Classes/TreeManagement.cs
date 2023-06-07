using Katzebase.PublicLibrary.Client;
using static Katzebase.UI.Classes.Constants;

namespace Katzebase.UI.Classes
{
    public static class TreeManagement
    {
        public static void PopulateServer(TreeView treeView, string serverAddress)
        {
            var client = new KatzebaseClient(serverAddress);
            if (client.Server.Ping() == false)
            {
                throw new Exception("Could not api ping the server.");
            }

            string key = serverAddress.ToLower();

            var foundNode = FindNodeOfType(treeView, ServerNodeType.Server, key);
            if (foundNode != null)
            {
                treeView.Nodes.Remove(foundNode);
            }

            var serverNode = CreateServerNode(key, serverAddress);

            PopulateSchemaNode(serverNode, client, ":");

            treeView.Nodes.Add(serverNode);
        }

        /// <summary>
        /// Populates a schema, its indexes and one level deeper to ensure there is somehting to expand in the tree.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static void PopulateSchemaNode(ServerTreeNode nodeToPopulate, KatzebaseClient client, string schema)
        {
            var schemaIndexes = client.Schema.Indexes.List(schema);
            var schemaIndexesNode = CreateIndexFolderNode();
            foreach (var index in schemaIndexes.List)
            {
                schemaIndexesNode.Nodes.Add(CreateIndexNode(index.Name));
            }
            nodeToPopulate.Nodes.Add(schemaIndexesNode);

            var schemas = client.Schema.List(schema);
            foreach (var item in schemas.List)
            {
                var schemaNode = CreateSchemaNode(item.Name ?? "");
                schemaNode.Nodes.Add(CreateTreeNotLoadedNode());
                nodeToPopulate.Nodes.Add(schemaNode);
            }
        }

        public static void PopulateSchemaNodeOnExpand(TreeView treeView, ServerTreeNode node)
        {
            //We only populate of the node does not contain schemas.
            if (node.Nodes.OfType<ServerTreeNode>().Where(o => o.NodeType == ServerNodeType.Schema).Any())
            {
                return;
            }

            node.Nodes.Clear();

            var rootNode = GetRootNode(node);
            var client = new KatzebaseClient(rootNode.ServerAddress);
            string schema = CalculateFullSchema(node);
            PopulateSchemaNode(node, client, schema);
        }

        #region Tree node factories.

        public static ServerTreeNode CreateSchemaNode(string name)
        {
            var node = new ServerTreeNode(name)
            {
                NodeType = Constants.ServerNodeType.Schema,
                ImageKey = "Schema",
                SelectedImageKey = "Schema"
            };

            return node;
        }

        public static ServerTreeNode CreateServerNode(string name, string serverAddress)
        {
            var node = new ServerTreeNode(name)
            {
                NodeType = Constants.ServerNodeType.Server,
                ImageKey = "Server",
                SelectedImageKey = "Server",
                ServerAddress = serverAddress
            };

            return node;
        }

        public static ServerTreeNode CreateTreeNotLoadedNode()
        {
            var node = new ServerTreeNode("TreeNotLoaded")
            {
                NodeType = Constants.ServerNodeType.TreeNotLoaded,
                ImageKey = "TreeNotLoaded",
                SelectedImageKey = "TreeNotLoaded"
            };

            return node;
        }

        public static ServerTreeNode CreateIndexFolderNode()
        {
            var node = new ServerTreeNode("Indexes")
            {
                NodeType = Constants.ServerNodeType.IndexFolder,
                ImageKey = "IndexFolder",
                SelectedImageKey = "IndexFolder"
            };

            return node;
        }

        public static ServerTreeNode CreateIndexNode(string name)
        {
            var node = new ServerTreeNode(name)
            {
                NodeType = Constants.ServerNodeType.Index,
                ImageKey = "Index",
                SelectedImageKey = "Index"
            };

            return node;
        }

        #endregion

        public static string CalculateFullSchema(ServerTreeNode node)
        {
            string result = node.Text;

            while (node.Parent != null && (node.Parent as ServerTreeNode)?.NodeType == ServerNodeType.Schema)
            {
                node = (ServerTreeNode)node.Parent;
                result = $"{node.Text}:{result}";
            }

            return result;
        }

        public static ServerTreeNode GetRootNode(ServerTreeNode node)
        {
            while (node.Parent != null)
            {
                node = (ServerTreeNode)node.Parent;
            }
            return node;
        }

        public static ServerTreeNode? FindNodeOfType(TreeView treeView, ServerNodeType type, string text)
        {
            foreach (var node in treeView.Nodes.OfType<ServerTreeNode>())
            {
                var result = FindNodeOfType(node, type, text);
                if (result != null)
                {
                    return result;
                }

                if (node.NodeType == type && node.Text == text)
                {
                    return node;
                }
            }
            return null;
        }

        public static ServerTreeNode? FindNodeOfType(ServerTreeNode rootNode, ServerNodeType type, string text)
        {
            foreach (var node in rootNode.Nodes.OfType<ServerTreeNode>())
            {
                var result = FindNodeOfType(node, type, text);
                if (result != null)
                {
                    return result;
                }

                if (node.NodeType == type && node.Text == text)
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
    }
}
