using NTDLS.Katzebase.Client.Payloads;
using static NTDLS.Katzebase.Management.Classes.Constants;

namespace NTDLS.Katzebase.Management.Classes
{
    public class ServerExplorerNode : TreeNode
    {
        public ServerNodeType NodeType { get; set; }

        /// <summary>
        /// The schema as it exists on the server, only populated when NodeType = ServerNodeType.Schema.
        /// </summary>
        public KbSchema? Schema { get; set; }

        /// <summary>
        /// The index as it exists on the server, only populated when NodeType = ServerNodeType.Index.
        /// </summary>
        public KbIndex? SchemaIndex { get; set; }

        /// <summary>
        /// Only populated when NodeType = ServerNodeType.Server.
        /// </summary>
        public ServerExplorerConnection? ExplorerManager { get; set; }

        public ServerExplorerNode(ServerNodeType nodeType, string name) :
            base(name)
        {
            NodeType = nodeType;
            Name = name;

            var imageKey = nodeType switch
            {
                ServerNodeType.Folder => "Folder",
                ServerNodeType.None => "TreeNotLoaded",
                ServerNodeType.Schema => "Schema",
                ServerNodeType.SchemaField => "SchemaField",
                ServerNodeType.SchemaFieldFolder => "SchemaFieldFolder",
                ServerNodeType.SchemaIndex => "SchemaIndex",
                ServerNodeType.SchemaIndexFolder => "SchemaIndexFolder",
                ServerNodeType.Server => "Server",
                _ => throw new Exception("Unsupported node type.")
            };

            ImageKey = imageKey;
            SelectedImageKey = imageKey;
        }

        public static ServerExplorerNode CreateServerNode(ServerExplorerConnection explorerManager)
            => new(ServerNodeType.Server, explorerManager.ServerAddress)
            {
                ExplorerManager = explorerManager
            };

        public static ServerExplorerNode CreateSchemaNode(KbSchema schema)
            => new(ServerNodeType.Schema, schema.Name)
            {
                Schema = schema,
            };

        public static ServerExplorerNode CreateSchemaIndexFolderNode()
            => new(ServerNodeType.SchemaIndexFolder, "Indexes");

        public static ServerExplorerNode CreateSchemaIndexNode(KbIndex schemaIndex)
            => new(ServerNodeType.SchemaIndex, schemaIndex.Name)
            {
                SchemaIndex = schemaIndex
            };
    }
}
