using NTDLS.Helpers;
using NTDLS.Katzebase.Management.Controls;
using static NTDLS.Katzebase.Management.Classes.Constants;

namespace NTDLS.Katzebase.Management.Classes
{
    /// <summary>
    /// Helper classes for working with the server explorer tree as a whole.
    /// </summary>
    public class ServerExplorerManager
    {
        public FormStudio StudioForm { get; private set; }
        public DoubleBufferedTreeView ServerExplorerTree { get; set; }
        public ServerExplorerNode? LastSelectedNode { get; set; }

        public ServerExplorerManager(FormStudio studioForm, DoubleBufferedTreeView serverExplorerTree)
        {
            StudioForm = studioForm;
            ServerExplorerTree = serverExplorerTree;

            serverExplorerTree.BeforeSelect += (object? sender, TreeViewCancelEventArgs e) =>
            {
                //Keep track of the last node that was selected, we will use this to
                //  determine which server we connect to when the user opens a new tab.
                if (e.Node is ServerExplorerNode selectedServerExplorerNode)
                {
                    LastSelectedNode = selectedServerExplorerNode;
                }
            };
        }

        public void DisconnectAll()
        {
            foreach (var serverNode in ServerExplorerTree.Nodes.OfType<ServerExplorerNode>())
            {
                serverNode.ExplorerConnection?.Disconnect();
            }
        }

        /// <summary>
        /// Finds an existing connected server explorer server node based on the given connection details.
        /// </summary>
        public ServerExplorerNode? FindServerNode(string serverAddress, int serverPort, string username)
        {
            foreach (var serverNode in ServerExplorerTree.Nodes.OfType<ServerExplorerNode>())
            {
                if (serverNode.ExplorerConnection != null)
                {
                    if (serverNode.ExplorerConnection.ServerAddress.Is(serverAddress)
                        && serverNode.ExplorerConnection.ServerPort == serverPort
                        && serverNode.ExplorerConnection.Username.Is(username))
                    {
                        return serverNode;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Walks up the tree nodes to obtain the server node for a given node.
        /// </summary>
        public static ServerExplorerNode? GetServerNodeFor(ServerExplorerNode node)
        {
            while (node?.Parent != null)
            {
                node = (ServerExplorerNode)node.Parent;
            }

            if (node?.NodeType == ServerNodeType.Server)
            {
                return node;
            }

            return null;
        }

        /// <summary>
        /// Returns the first schema encountered when walking up the tree from the given node.
        /// </summary>
        public static ServerExplorerNode? GetSchemaNodeFor(ServerExplorerNode? node)
        {
            do
            {
                if (node?.NodeType == ServerNodeType.Schema)
                {
                    return node;
                }

                node = (ServerExplorerNode?)node?.Parent;
            }
            while (node?.Parent != null);

            return null;
        }

        /// <summary>
        /// Walks up the tree nodes to obtain the server node for the last selected node.
        /// </summary>
        /// <returns></returns>
        public ServerExplorerNode? GetServerNodeForLastSelectedNode()
        {
            var node = LastSelectedNode;

            while (node?.Parent != null)
            {
                node = (ServerExplorerNode)node.Parent;
            }

            if (node?.NodeType == ServerNodeType.Server)
            {
                return node;
            }

            return null;
        }

        public static ServerExplorerNode? GetFirstChildNodeOfType(ServerExplorerNode givenNode, ServerNodeType nodeType)
            => givenNode.Nodes.OfType<ServerExplorerNode>().Where(o => o.NodeType == nodeType).FirstOrDefault();

        public static List<ServerExplorerNode> GetSingleLevelChildNodesOfType(ServerExplorerNode givenNode, ServerNodeType nodeType)
            => givenNode.Nodes.OfType<ServerExplorerNode>().Where(o => o.NodeType == nodeType).ToList();

        public static void SortChildNodes(TreeNode parentNode)
        {
            ServerNodeType[] sortFirst = {
                ServerNodeType.SchemaFieldsFolder,
                ServerNodeType.Folder,
                ServerNodeType.SchemaIndexFolder
            };

            if (parentNode.Nodes.Count > 0)
            {
                // Copy the child nodes to an array
                TreeNode[] childNodes = new TreeNode[parentNode.Nodes.Count];
                parentNode.Nodes.CopyTo(childNodes, 0);

                // Sort the child nodes by their Text property
                var sortedNodes = childNodes.OfType<ServerExplorerNode>()
                    .OrderBy(n => sortFirst.Contains(n.NodeType) ? $"000{n.Text}" : n.Text).ToArray();

                // Clear the existing nodes and re-add them in sorted order
                parentNode.Nodes.Clear();
                parentNode.Nodes.AddRange(sortedNodes);
            }
        }
    }
}
