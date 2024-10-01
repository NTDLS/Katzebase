using NTDLS.Helpers;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Management.StaticAnalysis;
using static NTDLS.Katzebase.Management.Classes.Constants;

namespace NTDLS.Katzebase.Management.Classes
{
    /// <summary>
    /// Static helpers for dealing with the server explorer tree view and its nodes.
    /// </summary>
    public static class TreeManagement
    {
        public static void PopulateServer(TreeView treeView, string serverAddress, int serverPort, string username, string passwordHash)
        {
            var client = new KbClient(serverAddress, serverPort, username, passwordHash, $"{Client.KbConstants.FriendlyName}.UI");
            client.QueryTimeout = TimeSpan.FromSeconds(Program.Settings.UIQueryTimeOut);
            client.OnDisconnected += (KbClient sender, Client.Payloads.KbSessionInfo sessionInfo) =>
            {
                BackgroundSchemaCache.Instance.Stop();
            };

            BackgroundSchemaCache.Instance.StartOrReset(client);

            string key = serverAddress.ToLower();

            var foundNode = FindNodeOfType(treeView, ServerNodeType.Server, key);
            if (foundNode != null)
            {
                treeView.Nodes.Remove(foundNode);
            }

            var serverNode = CreateServerNode(key, serverAddress, serverPort, username, passwordHash, client);

            PopulateSchemaNode(serverNode, client, ":");

            treeView.Nodes.Add(serverNode);
        }

        private static void Client_OnConnected(KbClient sender, Client.Payloads.KbSessionInfo sessionInfo)
        {
            throw new NotImplementedException();
        }

        public static void Close(TreeView treeView)
        {
            BackgroundSchemaCache.Instance.Stop();

            var rootNode = GetRootNode(treeView);
            if (rootNode != null)
            {
                Exceptions.Ignore(() => rootNode.ServerClient?.Dispose());
            }
        }

        /// <summary>
        /// Populates a schema, its indexes and one level deeper to ensure there is something to expand in the tree.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static void PopulateSchemaNode(ServerExplorerNode nodeToPopulate, KbClient client, string schema)
        {
            var schemaIndexes = client.Schema.Indexes.List(schema);
            var schemaIndexesNode = CreateIndexFolderNode();
            foreach (var index in schemaIndexes.List)
            {
                schemaIndexesNode.Nodes.Add(CreateIndexNode(index.Name));
            }
            nodeToPopulate.Nodes.Add(schemaIndexesNode);

            var schemaFields = client.Document.List(schema, 1);
            var schemaFieldNode = CreateFieldFolderNode();
            foreach (var field in schemaFields.Fields)
            {
                schemaFieldNode.Nodes.Add(CreateFieldNode(field.Name));
            }
            nodeToPopulate.Nodes.Add(schemaFieldNode);

            var schemas = client.Schema.List(schema);
            foreach (var item in schemas.Collection)
            {
                var schemaNode = CreateSchemaNode(item.Name ?? "");
                schemaNode.Nodes.Add(CreateTreeNotLoadedNode());
                nodeToPopulate.Nodes.Add(schemaNode);
            }
        }

        public static void PopulateSchemaNodeOnExpand(TreeView treeView, ServerExplorerNode node)
        {
            //We only populate nodes that do not contain schemas.
            if (node.Nodes.OfType<ServerExplorerNode>().Where(o => o.NodeType == ServerNodeType.Schema).Any())
            {
                return;
            }

            var rootNode = GetRootNode(node);
            string schema = FullSchemaPath(node);

            node.Nodes.Clear(); //Don't clear the node until we hear back from the server.
            if (rootNode.ServerClient == null || !rootNode.ServerClient.IsConnected)
            {
                PopulateServer(treeView, rootNode.ServerAddress, rootNode.ServerPort, rootNode.Username, rootNode.PasswordHash);
                return;
            }

            PopulateSchemaNode(node, rootNode.ServerClient, schema);
        }

        #region Tree node factories.

        public static ServerExplorerNode CreateSchemaNode(string name)
        {
            var node = new ServerExplorerNode(name)
            {
                NodeType = Constants.ServerNodeType.Schema,
                ImageKey = "Schema",
                SelectedImageKey = "Schema"
            };

            return node;
        }

        public static ServerExplorerNode CreateServerNode(string name, string serverAddress, int serverPort, string username, string passwordHash, KbClient serverClient)
        {
            var node = new ServerExplorerNode(name)
            {
                NodeType = Constants.ServerNodeType.Server,
                ImageKey = "Server",
                SelectedImageKey = "Server",
                ServerAddress = serverAddress,
                ServerPort = serverPort,
                Username = username,
                PasswordHash = passwordHash,
                ServerClient = serverClient
            };

            return node;
        }

        public static ServerExplorerNode CreateTreeNotLoadedNode()
        {
            var node = new ServerExplorerNode("TreeNotLoaded")
            {
                NodeType = Constants.ServerNodeType.TreeNotLoaded,
                ImageKey = "TreeNotLoaded",
                SelectedImageKey = "TreeNotLoaded"
            };

            return node;
        }

        public static ServerExplorerNode CreateIndexFolderNode()
        {
            var node = new ServerExplorerNode("Indexes")
            {
                NodeType = Constants.ServerNodeType.IndexFolder,
                ImageKey = "IndexFolder",
                SelectedImageKey = "IndexFolder"
            };

            return node;
        }

        public static ServerExplorerNode CreateFieldFolderNode()
        {
            var node = new ServerExplorerNode("Fields")
            {
                NodeType = Constants.ServerNodeType.FieldFolder,
                ImageKey = "FieldFolder",
                SelectedImageKey = "FieldFolder"
            };

            return node;
        }

        public static ServerExplorerNode CreateIndexNode(string name)
        {
            var node = new ServerExplorerNode(name)
            {
                NodeType = Constants.ServerNodeType.Index,
                ImageKey = "Index",
                SelectedImageKey = "Index"
            };

            return node;
        }

        public static ServerExplorerNode CreateFieldNode(string name)
        {
            var node = new ServerExplorerNode(name)
            {
                NodeType = Constants.ServerNodeType.Field,
                ImageKey = "Field",
                SelectedImageKey = "Field"
            };

            return node;
        }

        #endregion

        public static string FullSchemaPath(ServerExplorerNode node)
        {
            string result = string.Empty;

            if (node is ServerExplorerNode { NodeType: ServerNodeType.Schema })
            {
                result = node.Text;
            }

            while (node.Parent != null && (node.Parent as ServerExplorerNode)?.NodeType != ServerNodeType.Schema)
            {
                node = (ServerExplorerNode)node.Parent;
            }

            while (node.Parent != null && node.Parent is ServerExplorerNode { NodeType: ServerNodeType.Schema })
            {
                node = (ServerExplorerNode)node.Parent;
                result = $"{node.Text}:{result}";
            }

            return result.Trim([':']);
        }

        public static ServerExplorerNode? GetRootNode(TreeView treeView)
        {
            if (treeView.Nodes.Count > 0 && treeView.Nodes[0] is ServerExplorerNode treeNode)
            {
                return GetRootNode(treeNode);
            }
            return null;
        }

        public static ServerExplorerNode GetRootNode(ServerExplorerNode node)
        {
            while (node.Parent != null)
            {
                node = (ServerExplorerNode)node.Parent;
            }
            return node;
        }

        public static ServerExplorerNode? FindNodeOfType(TreeView treeView, ServerNodeType type, string text)
        {
            foreach (var node in treeView.Nodes.OfType<ServerExplorerNode>())
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

        public static ServerExplorerNode? FindNodeOfType(ServerExplorerNode rootNode, ServerNodeType type, string text)
        {
            foreach (var node in rootNode.Nodes.OfType<ServerExplorerNode>())
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

                    node.Nodes.Insert(nodeIndex + 1, nodeCopy.EnsureNotNull());
                    moves++;
                }
                else if (node.Nodes[i + 1].Text.CompareTo(node.Nodes[i].Text) < 0)
                {
                    int nodeIndex = node.Nodes[i].Index;
                    var nodeCopy = node.Nodes[i].Clone() as TreeNode;
                    node.Nodes.Remove(node.Nodes[i]);

                    node.Nodes.Insert(nodeIndex - 1, nodeCopy.EnsureNotNull());
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
