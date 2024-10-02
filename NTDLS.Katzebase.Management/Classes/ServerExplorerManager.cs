using NTDLS.Helpers;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Management.Properties;
using NTDLS.Katzebase.Management.StaticAnalysis;
using NTDLS.Katzebase.Shared;
using static NTDLS.Katzebase.Management.Classes.Constants;

namespace NTDLS.Katzebase.Management.Classes
{
    /// <summary>
    /// Static helpers for dealing with the server explorer tree view and its nodes.
    /// </summary>
    public static class ServerExplorerManager
    {
        public static string? ServerAddress { get; private set; }
        public static int ServerPort { get; private set; }
        public static string? Username { get; private set; }
        public static string? PasswordHash { get; private set; }

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
            ServerAddress = serverAddress;
            ServerPort = serverPort;
            Username = username;
            PasswordHash = passwordHash;

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
            var rootSchemaNode = ServerExplorerNode.CreateSchemaNode(new(EngineConstants.RootSchemaGUID, "Root :", "", "", Guid.Empty, 100));
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
                    var existingNode = FindNodeBySchemaId(schemaItem.Schema.Id);
                    if (existingNode != null)
                    {
                        return;
                    }

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
                    SortChildNodes(parentSchemaNode); //Sort the indexes.
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
                    bool schemaNameChanged = existingSchemaNode.Text != schemaItem.Schema.Name;
                    existingSchemaNode.Schema = schemaItem.Schema;

                    if (schemaNameChanged)
                    {
                        existingSchemaNode.Text = schemaItem.Schema.Name;
                        SortChildNodes(existingSchemaNode.Parent); //Sort the schemas.
                    }

                    var schemaIndexFolderNode = GetFirstChildNodeOfType(existingSchemaNode, ServerNodeType.SchemaIndexFolder);
                    if (schemaIndexFolderNode != null)
                    {
                        var existingSchemaIndexNodes = GetSingleLevelChildNodesOfType(schemaIndexFolderNode, ServerNodeType.SchemaIndex);

                        bool indexNameChanged = false;
                        bool indexAdded = false;

                        //Add/update indexes to the tree.
                        foreach (var serverSchemaIndex in schemaItem.Indexes)
                        {
                            var existingSchemaIndexNode = existingSchemaIndexNodes.FirstOrDefault(o => o.SchemaIndex?.Id == serverSchemaIndex.Id);
                            if (existingSchemaIndexNode == null)
                            {
                                //Add newly discovered schema index to the tree.
                                var schemaIndexNode = ServerExplorerNode.CreateSchemaIndexNode(serverSchemaIndex);
                                schemaIndexFolderNode.Nodes.Add(schemaIndexNode);
                                indexAdded = true;
                            }
                            else
                            {
                                //Refresh existing schema index in the tree.
                                indexNameChanged = indexNameChanged || (existingSchemaIndexNode.Text != serverSchemaIndex.Name);

                                if (indexNameChanged)
                                {
                                    existingSchemaIndexNode.Text = serverSchemaIndex.Name;
                                }
                                existingSchemaIndexNode.SchemaIndex = serverSchemaIndex;
                            }
                        }

                        //Remove indexes from the tree which are no longer present on the server.
                        foreach (var existingSchemaIndexNode in existingSchemaIndexNodes)
                        {
                            if (schemaItem.Indexes.Any(o => o.Id == existingSchemaIndexNode.SchemaIndex?.Id) == false)
                            {
                                schemaIndexFolderNode.Nodes.Remove(existingSchemaIndexNode);
                            }
                        }

                        if (indexNameChanged || indexAdded)
                        {
                            SortChildNodes(schemaIndexFolderNode);
                        }
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

        public static void SortChildNodes(TreeNode parentNode)
        {
            ServerNodeType[] sortFirst = {
                ServerNodeType.SchemaFieldFolder,
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
