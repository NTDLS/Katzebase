using NTDLS.Katzebase.Client.Payloads;
using static NTDLS.Katzebase.Management.Classes.Constants;

namespace NTDLS.Katzebase.Management.Classes
{
    public class ServerExplorerNode : TreeNode
    {
        public ServerNodeType NodeType { get; set; }

        public KbSchema? Schema { get; set; }

        public ServerExplorerNode(ServerNodeType nodeType, string name) :
            base(name)
        {
            NodeType = nodeType;
            Name = name;

            var imageKey = nodeType switch
            {
                ServerNodeType.Field => "Field",
                ServerNodeType.FieldFolder => "FieldFolder",
                ServerNodeType.Folder => "Folder",
                ServerNodeType.Index => "Index",
                ServerNodeType.IndexFolder => "IndexFolder",
                ServerNodeType.None => "TreeNotLoaded",
                ServerNodeType.Schema => "Schema",
                ServerNodeType.Server => "Server",
                _ => throw new Exception("Unsupported node type.")
            };

            ImageKey = imageKey;
            SelectedImageKey = imageKey;
        }

        public static ServerExplorerNode CreateServerNode(string name)
            => new ServerExplorerNode(ServerNodeType.Server, name);

        public static ServerExplorerNode CreateSchemaNode(KbSchema schema)
            => new ServerExplorerNode(ServerNodeType.Schema, schema.Name)
            {
                Schema = schema,
            };
    }
}
