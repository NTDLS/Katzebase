using static Katzebase.UI.Classes.Constants;

namespace Katzebase.UI.Classes
{
    public class ProjectTreeNode : TreeNode
    {
        public string FullFilePath { get; set; } = string.Empty;
        public ProjectNodeType NodeType { get; set; }

        public ProjectTreeNode(string name) :
             base(name)
        {
        }

        public string? ConfigFilePath
        {
            get
            {
                if (NodeType == ProjectNodeType.Workloads || NodeType == ProjectNodeType.Workload)
                {
                    return $"{FullFilePath}\\@Configuration.txt";
                }
                return null;
            }
        }

        public string GetDefaultFileContents()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults");

#if DEBUG
            path = @"C:\C:\NTDLS\Katzebase\Installers\Syntax Highlighters\Defaults";
#endif

            switch (NodeType)
            {
                case ProjectNodeType.Script:
                case ProjectNodeType.Workload:
                    path = Path.Combine(path, $"{NodeType}.txt");
                    break;
            }

            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            return string.Empty;
        }

        /// <summary>
        /// Renames the file and returns the new full path.
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public string Rename(string newName)
        {
            if (NodeType == ProjectNodeType.Asset
                || NodeType == ProjectNodeType.Script
                || NodeType == ProjectNodeType.Note)
            {
                string oldPath = FullFilePath;
                var oldDirectory = Path.GetDirectoryName(oldPath);
                if (oldDirectory == null)
                {
                    throw new ArgumentException("Invalid directory name.");
                }

                FullFilePath = Path.Combine(oldDirectory, newName);

                File.Move(oldPath, FullFilePath);

                return FullFilePath;
            }
            else if (NodeType == ProjectNodeType.Workload)
            {
                string oldPath = FullFilePath;
                var oldDirectory = Path.GetDirectoryName(oldPath);
                if (oldDirectory == null)
                {
                    throw new ArgumentException("Invalid directory name.");
                }

                FullFilePath = Path.Combine(oldDirectory, newName);

                Directory.Move(oldPath, FullFilePath);

                return FullFilePath;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public string GetUniqueFileName(ProjectNodeType nodeType = ProjectNodeType.None)
        {
            if (nodeType == ProjectNodeType.None)
            {
                nodeType = NodeType;
            }

            string extension;
            switch (nodeType)
            {
                case ProjectNodeType.Asset:
                case ProjectNodeType.Note:
                    extension = ".txt";
                    break;
                case ProjectNodeType.Script:
                    extension = ".sql";
                    break;
                case ProjectNodeType.Workload:
                    extension = "";
                    break;
                default:
                    throw new NotImplementedException();
            }

            for (int i = 1; i < 100000; i++)
            {
                string path = Path.Combine(FullFilePath, $"New {nodeType} {i}{extension}");
                if (!File.Exists(path))
                {
                    return $"New {nodeType} {i}{extension}";
                }
            }

            return string.Empty;
        }

        public ProjectTreeNode AddScriptNode()
        {
            string filePath = Path.Combine(FullFilePath, GetUniqueFileName(ProjectNodeType.Script));
            var node = FormUtility.CreateScriptNode(filePath);
            Nodes.Add(node);

            var fileContents = node.GetDefaultFileContents();
            File.WriteAllText(node.FullFilePath, fileContents);

            return node;
        }

        public ProjectTreeNode AddNoteNode()
        {
            string filePath = Path.Combine(FullFilePath, GetUniqueFileName(ProjectNodeType.Note));
            var node = FormUtility.CreateNoteNode(filePath);
            Nodes.Add(node);

            File.WriteAllText(node.FullFilePath, string.Empty);
            return node;
        }

        public ProjectTreeNode AddAssetNode()
        {
            string filePath = Path.Combine(FullFilePath, GetUniqueFileName(ProjectNodeType.Asset));
            var node = FormUtility.CreateAssetNode(filePath);
            Nodes.Add(node);

            var fileContents = node.GetDefaultFileContents();
            File.WriteAllText(node.FullFilePath, fileContents);

            return node;
        }

        public ProjectTreeNode AddWorkloadNode()
        {
            string filePath = Path.Combine(FullFilePath, GetUniqueFileName(ProjectNodeType.Workload));
            var node = FormUtility.CreateWorkloadNode(filePath);
            Nodes.Add(node);

            Directory.CreateDirectory(filePath);

            var fileContents = node.GetDefaultFileContents();
            if (node.ConfigFilePath != null)
            {
                File.WriteAllText(node.ConfigFilePath, fileContents);
            }

            return node;
        }
    }
}
