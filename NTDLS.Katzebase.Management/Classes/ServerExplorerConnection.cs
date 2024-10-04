﻿using NTDLS.Helpers;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Management.StaticAnalysis;
using NTDLS.Katzebase.Shared;
using static NTDLS.Katzebase.Management.Classes.Constants;

namespace NTDLS.Katzebase.Management.Classes
{
    /// <summary>
    /// Used to support a server connection in the server explorer tree,
    /// an instance of this class is associated with each node where the type is Server.
    /// </summary>
    public class ServerExplorerConnection
    {
        public string ServerAddress { get; private set; }
        public int ServerPort { get; private set; }
        public string Username { get; private set; }
        public string PasswordHash { get; private set; }

        public ServerExplorerManager ServerExplorerManager { get; private set; }
        public FormStudio? StudioForm { get; private set; }
        public KbClient? Client { get; private set; }

        public LazyBackgroundSchemaCache LazySchemaCache { get; private set; }

        public ServerExplorerNode ServerNode { get; private set; }

        public ServerExplorerConnection(FormStudio formStudio, ServerExplorerManager serverExplorerManager, string serverAddress, int serverPort, string username, string passwordHash)
        {
            ServerExplorerManager = serverExplorerManager;
            ServerAddress = serverAddress;
            ServerPort = serverPort;
            Username = username;
            PasswordHash = passwordHash;

            Client = new KbClient(serverAddress, serverPort, username, passwordHash, $"{KbConstants.FriendlyName}.UI");
            Client.QueryTimeout = TimeSpan.FromSeconds(Program.Settings.UIQueryTimeOut);
            Client.OnDisconnected += (KbClient sender, Client.Payloads.KbSessionInfo sessionInfo) =>
            {
                LazySchemaCache?.Stop();
            };

            LazySchemaCache = new LazyBackgroundSchemaCache(this);
            LazySchemaCache.OnCacheUpdated += (List<CachedSchema> schemaCache) =>
            {
                StudioForm?.CurrentTabFilePage()?.Editor.InvokePerformStaticAnalysis();
            };

            LazySchemaCache.OnCacheItemAdded += SchemaCache_OnCacheItemAdded;
            LazySchemaCache.OnCacheItemRemoved += SchemaCache_OnCacheItemRemoved;
            LazySchemaCache.OnCacheItemRefreshed += SchemaCache_OnCacheItemRefreshed;

            ServerNode = ServerExplorerNode.CreateServerNode(this);
            var rootSchemaNode = ServerExplorerNode.CreateSchemaNode(new(EngineConstants.RootSchemaGUID, "Root :", "", "", Guid.Empty, 100));
            ServerNode.Nodes.Add(rootSchemaNode);
            ServerExplorerManager.ServerExplorerTree.Nodes.Add(ServerNode);

            ServerExplorerManager.ServerExplorerTree.SelectedNode = rootSchemaNode;
        }

        /// <summary>
        /// Creates a new connection based on the explorer connection.
        /// </summary>
        /// <returns></returns>
        public KbClient CreateNewConnection()
        {
            var client = new KbClient(ServerAddress, ServerPort, Username, PasswordHash, $"{KbConstants.FriendlyName}.UI");
            client.QueryTimeout = TimeSpan.FromSeconds(Program.Settings.UIQueryTimeOut);

            return client;
        }

        public void Disconnect()
        {
            LazySchemaCache.Stop();
            Exceptions.Ignore(() => Client?.Dispose());
        }

        private void SchemaCache_OnCacheItemAdded(CachedSchema schemaItem)
        {
            ServerExplorerManager.ServerExplorerTree.Invoke(() =>
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
                    ServerExplorerManager.SortChildNodes(parentSchemaNode); //Sort the indexes.
                }
            });
        }

        private void SchemaCache_OnCacheItemRefreshed(CachedSchema schemaItem)
        {
            ServerExplorerManager.ServerExplorerTree.Invoke(() =>
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
                        ServerExplorerManager.SortChildNodes(existingSchemaNode.Parent); //Sort the schemas.
                    }

                    var schemaIndexFolderNode = ServerExplorerManager.GetFirstChildNodeOfType(existingSchemaNode, ServerNodeType.SchemaIndexFolder);
                    if (schemaIndexFolderNode != null)
                    {
                        var existingSchemaIndexNodes = ServerExplorerManager.GetSingleLevelChildNodesOfType(schemaIndexFolderNode, ServerNodeType.SchemaIndex);

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
                            ServerExplorerManager.SortChildNodes(schemaIndexFolderNode);
                        }
                    }
                }
            });
        }

        private void SchemaCache_OnCacheItemRemoved(CachedSchema schemaItem)
        {
            ServerExplorerManager.ServerExplorerTree.Invoke(() =>
            {
                var removedSchemaNode = FindNodeBySchemaId(schemaItem.Schema.Id);
                removedSchemaNode?.Remove();
            });
        }

        public ServerExplorerNode? FindNodeBySchemaId(Guid schemaId)
        {
            foreach (var node in ServerNode.Nodes.OfType<ServerExplorerNode>())
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

            ServerExplorerNode? FindNodeBySchemaIdRecursive(ServerExplorerNode rootNode, Guid schemaId)
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
    }
}