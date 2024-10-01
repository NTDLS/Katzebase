using static NTDLS.Katzebase.Management.Classes.Constants;

namespace NTDLS.Katzebase.Management.Classes
{
    public class ServerExplorerNode : TreeNode
    {
        public ServerNodeType NodeType { get; set; }

        public string? SchemaPath { get; set; }
        public Guid SchemaId { get; set; }
        public Guid ParentSchemaId { get; set; }

        public ServerExplorerNode(ServerNodeType nodeType, string name) :
            base(name)
        {
            NodeType = nodeType;
            Name = name;
        }

        public static ServerExplorerNode CreateServerNode(string name)
        {
            return new ServerExplorerNode(ServerNodeType.Server, name)
            {
                NodeType = ServerNodeType.Server,
                ImageKey = "Server",
                SelectedImageKey = "Server",
            };
        }

        public static ServerExplorerNode CreateSchemaNode(string schemaName, string schemaPath, Guid parentSchemaId, Guid schemaId)
        {
            return new ServerExplorerNode(ServerNodeType.Server, schemaName)
            {
                NodeType = ServerNodeType.Schema,
                ImageKey = "Schema",
                SelectedImageKey = "Schema",
                SchemaPath = schemaPath,
                SchemaId = schemaId,
                ParentSchemaId = parentSchemaId,
            };
        }
    }
}
