namespace Katzebase.UI.Classes
{
    internal class Project
    {
        public bool IsLoaded { get; private set; } = false;
        public string ProjectFile { get; set; } = string.Empty;
        public string ProjectPath { get; set; } = string.Empty;
        public string AssetsPath { get; set; } = string.Empty;
        public string WorkloadsPath { get; set; } = string.Empty;

        public Project()
        {
            IsLoaded = false;
        }

        public Project(string projectFile)
        {
            var projectpath = Path.GetDirectoryName(projectFile)?.TrimEnd(new char[] { '\\', '/' });
            if (projectpath == null)
            {
                throw new Exception("Invalud project path.");
            }

            ProjectPath = projectpath;
            ProjectFile = projectFile;
            AssetsPath = $"{projectpath}\\Assets";
            WorkloadsPath = $"{projectpath}\\Workloads";
        }

        public static Project Create(string projectFilePath)
        {
            var project = new Project(projectFilePath);

            Directory.CreateDirectory(project.ProjectPath);
            Directory.CreateDirectory(project.WorkloadsPath);
            Directory.CreateDirectory(project.AssetsPath);
            File.WriteAllText(project.ProjectFile, "");

            Preferences.Instance.AddRecentProject(projectFilePath);
            Preferences.Save();

            project.IsLoaded = true;

            return project;
        }

        public static Project Load(string projectFile, TreeView treeView)
        {
            treeView.Nodes.Clear();

            var project = new Project(projectFile)
            {
                IsLoaded = true
            };
            var rootNode = FormUtility.CreateProjectNode(project.ProjectPath);

            var notes = Directory.EnumerateFiles(project.ProjectPath, "*.txt").ToList();
            foreach (var note in notes)
            {
                var node = FormUtility.CreateNoteNode(note);
                rootNode.Nodes.Add(node);
            }

            var assetsNode = FormUtility.CreateAssetsNode(project.AssetsPath);
            rootNode.Nodes.Add(assetsNode);

            var assets = Directory.EnumerateFiles(project.AssetsPath, "*.txt").ToList();
            foreach (var asset in assets)
            {
                var node = FormUtility.CreateAssetNode(asset);
                assetsNode.Nodes.Add(node);
            }

            var workloadsNode = FormUtility.CreateWorkloadsNode(project.WorkloadsPath);
            rootNode.Nodes.Add(workloadsNode);

            var workloads = Directory.EnumerateDirectories(project.WorkloadsPath).ToList();
            foreach (var workload in workloads)
            {
                var workloadNode = FormUtility.CreateWorkloadNode(workload);
                workloadsNode.Nodes.Add(workloadNode);

                var scripts = Directory.EnumerateFiles(workload, "*.sql").ToList();
                foreach (var script in scripts)
                {
                    var scriptNode = FormUtility.CreateScriptNode(script);
                    workloadNode.Nodes.Add(scriptNode);
                }
            }

            treeView.Nodes.Add(rootNode);
            rootNode.Expand();

            Preferences.Instance.AddRecentProject(projectFile);
            Preferences.Save();

            return project;
        }
    }
}