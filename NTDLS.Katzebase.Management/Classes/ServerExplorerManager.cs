using NTDLS.Helpers;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Management.Properties;
using NTDLS.Katzebase.Management.StaticAnalysis;
using NTDLS.Katzebase.Shared;
using System;
using static NTDLS.Katzebase.Management.Classes.Constants;

namespace NTDLS.Katzebase.Management.Classes
{
    /// <summary>
    /// Static helpers for dealing with the server explorer tree view and its nodes.
    /// </summary>
    public static class ServerExplorerManager
    {
        public static TreeView? ServerExplorerTree { get; private set; }
        public static FormStudio? StudioForm { get; private set; }
        public static KbClient? Client { get; private set; }

        private static readonly ImageList _treeImages = new();

        public static void Initialize(FormStudio formStudio, TreeView serverExplorerTree)
        {
            StudioForm = formStudio;
            ServerExplorerTree = serverExplorerTree;

            _treeImages.ColorDepth = ColorDepth.Depth32Bit;
            _treeImages.Images.Add("Folder", Resources.TreeFolder);
            _treeImages.Images.Add("Schema", Resources.TreeSchema);
            _treeImages.Images.Add("SchemaField", Resources.TreeField);
            _treeImages.Images.Add("SchemaFieldFolder", Resources.TreeDocument);
            _treeImages.Images.Add("SchemaIndex", Resources.TreeIndex);
            _treeImages.Images.Add("SchemaIndexFolder", Resources.TreeIndexFolder);
            _treeImages.Images.Add("Server", Resources.TreeServer);
            _treeImages.Images.Add("TreeNotLoaded", Resources.TreeNotLoaded);
            ServerExplorerTree.ImageList = _treeImages;
        }

        public static void Connect(string serverAddress, int serverPort, string username, string passwordHash)
        {
            Client = new KbClient(serverAddress, serverPort, username, passwordHash, $"{KbConstants.FriendlyName}.UI");
            Client.QueryTimeout = TimeSpan.FromSeconds(Program.Settings.UIQueryTimeOut);
            Client.OnDisconnected += (KbClient sender, Client.Payloads.KbSessionInfo sessionInfo) =>
            {
                BackgroundSchemaCache.Stop();
            };

            BackgroundSchemaCache.StartOrReset(Client);
            BackgroundSchemaCache.OnCacheUpdated += (List<CachedSchema> schemaCache) =>
            {
                StudioForm?.CurrentTabFilePage()?.Editor.InvokePerformStaticAnalysis();
            };

            BackgroundSchemaCache.OnCacheItemAdded += BackgroundSchemaCache_OnCacheItemAdded;
            BackgroundSchemaCache.OnCacheItemRemoved += BackgroundSchemaCache_OnCacheItemRemoved;
            BackgroundSchemaCache.OnCacheItemRefreshed += BackgroundSchemaCache_OnCacheItemRefreshed;

            ServerExplorerTree.EnsureNotNull().Nodes.Clear();

            var serverNode = ServerExplorerNode.CreateServerNode(serverAddress);
            var rootSchemaNode = ServerExplorerNode.CreateSchemaNode(new(EngineConstants.RootSchemaGUID, "Root (:)", "", "", Guid.Empty, 100));
            serverNode.Nodes.Add(rootSchemaNode);
            ServerExplorerTree.Nodes.Add(serverNode);

            //PopulateSchemaNode(serverNode, ":");
            //ServerExplorerTree.EnsureNotNull().Nodes.Add(serverNode);
        }

        public static void Close(TreeView treeView)
        {
            BackgroundSchemaCache.Stop();
            Exceptions.Ignore(() => Client?.Dispose());
        }

        private static void BackgroundSchemaCache_OnCacheItemAdded(CachedSchema schemaItem)
        {
            ServerExplorerTree.EnsureNotNull().Invoke(() =>
            {
                var parentSchemaNode = FindNodeBySchemaId(schemaItem.Schema.ParentId);
                if (parentSchemaNode != null && parentSchemaNode.Schema != null)
                {
                    var newSchemaNode = ServerExplorerNode.CreateSchemaNode(schemaItem.Schema);
                    parentSchemaNode.Nodes.Add(newSchemaNode);

                    var schemaIndexFolderNode = ServerExplorerNode.CreateSchemaIndexFolderNode();
                    newSchemaNode.Nodes.Add(schemaIndexFolderNode);

                    foreach (var index in schemaItem.Indexes.OrderBy(o => o.Name))
                    {
                        var schemaIndexNode = ServerExplorerNode.CreateSchemaIndexNode(index);
                        schemaIndexFolderNode.Nodes.Add(schemaIndexNode);
                    }

                    if (parentSchemaNode.Schema.ParentId == Guid.Empty && parentSchemaNode.Nodes.Count == 1)
                    {
                        //Expand the root schema node when we add the first node.
                        parentSchemaNode.Parent.Expand();
                        parentSchemaNode.Expand();
                    }
                }
            });
        }

        private static void BackgroundSchemaCache_OnCacheItemRefreshed(CachedSchema schemaItem)
        {
            ServerExplorerTree.EnsureNotNull().Invoke(() =>
            {
                var existingSchemaNode = FindNodeBySchemaId(schemaItem.Schema.Id);
                if (existingSchemaNode != null)
                {
                    //Update basic schema information.
                    existingSchemaNode.Text = schemaItem.Schema.Name;
                    existingSchemaNode.Schema = schemaItem.Schema;

                    var schemaIndexFolderNode = GetFirstChildNodeOfType(existingSchemaNode, ServerNodeType.SchemaIndexFolder);
                    if (schemaIndexFolderNode != null)
                    {
                        var existingSchemaIndexNodes = GetSingleLevelChildNodesOfType(schemaIndexFolderNode, ServerNodeType.SchemaIndex);

                        //Add/update indexes to the tree.
                        foreach (var serverSchemaIndex in schemaItem.Indexes)
                        {
                            var existingSchemaIndexNode = existingSchemaIndexNodes.FirstOrDefault(o => o.SchemaIndex?.Id == serverSchemaIndex.Id);
                            if (existingSchemaIndexNode == null)
                            {
                                //Add newly discovered schema index to the tree.
                                var schemaIndexNode = ServerExplorerNode.CreateSchemaIndexNode(serverSchemaIndex);
                                schemaIndexFolderNode.Nodes.Add(schemaIndexNode);
                            }
                            else
                            {
                                //Refresh existing schema index in the tree.
                                existingSchemaIndexNode.Text = serverSchemaIndex.Name;
                                existingSchemaIndexNode.SchemaIndex = serverSchemaIndex;
                            }
                        }

                        //Remove indexes from the tree.
                        //var indexNodesToDelete = new List<ServerExplorerNode>();
                        foreach (var existingSchemaIndexNode in existingSchemaIndexNodes)
                        {
                            if (schemaItem.Indexes.Any(o => o.Id == existingSchemaIndexNode.SchemaIndex?.Id) == false)
                            {
                                schemaIndexFolderNode.Nodes.Remove(existingSchemaIndexNode);
                                //indexNodesToDelete.Add(existingSchemaIndexNode);
                            }
                        }

                        /*
                        foreach (var indexNodeToDelete in indexNodesToDelete)
                        {
                        }
                        */


                    }
                }
            });
        }

        private static void BackgroundSchemaCache_OnCacheItemRemoved(CachedSchema schemaItem)
        {
            ServerExplorerTree.EnsureNotNull().Invoke(() =>
            {
                var removedSchemaNode = FindNodeBySchemaId(schemaItem.Schema.Id);
                removedSchemaNode?.Remove();
            });
        }

        public static ServerExplorerNode? FindNodeBySchemaId(Guid schemaId)
        {
            foreach (var node in ServerExplorerTree.EnsureNotNull().Nodes.OfType<ServerExplorerNode>())
            {
                var result = FindNodeBySchemaIdRecursive(node, schemaId);
                if (result != null)
                {
                    return result;
                }

                if (node.NodeType == ServerNodeType.Schema && node.Schema?.Id == schemaId)
                {
                    return node;
                }
            }

            static ServerExplorerNode? FindNodeBySchemaIdRecursive(ServerExplorerNode rootNode, Guid schemaId)
            {
                foreach (var node in rootNode.Nodes.OfType<ServerExplorerNode>())
                {
                    var result = FindNodeBySchemaIdRecursive(node, schemaId);
                    if (result != null)
                    {
                        return result;
                    }

                    if (node.NodeType == ServerNodeType.Schema && node.Schema?.Id == schemaId)
                    {
                        return node;
                    }
                }
                return null;
            }

            return null;
        }

        public static ServerExplorerNode? GetFirstChildNodeOfType(ServerExplorerNode givenNode, ServerNodeType nodeType)
            => givenNode.Nodes.OfType<ServerExplorerNode>().Where(o => o.NodeType == nodeType).FirstOrDefault();

        public static List<ServerExplorerNode> GetSingleLevelChildNodesOfType(ServerExplorerNode givenNode, ServerNodeType nodeType)
            => givenNode.Nodes.OfType<ServerExplorerNode>().Where(o => o.NodeType == nodeType).ToList();

        /*

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
                Connect(rootNode.ServerAddress, rootNode.ServerPort, rootNode.Username, rootNode.PasswordHash);
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

        public static ServerExplorerNode? FindNodeOfType(ServerNodeType type, string text)
        {
            foreach (var node in ServerExplorerTree.EnsureNotNull().Nodes.OfType<ServerExplorerNode>())
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
        */
    }
}
