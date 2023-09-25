using Katzebase;
using Newtonsoft.Json;

namespace Katzebase.UI.Classes
{
    internal class Preferences
    {
        static private Preferences? _instance = null;
        static public Preferences Instance
        {
            get
            {
                EnsureInstanceIsCreated();
                KbUtility.EnsureNotNull(_instance);
                return _instance;
            }
        }

        public static void EnsureInstanceIsCreated()
        {
            if (_instance == null)
            {
                if (Directory.Exists(DirectoryPath) == false)
                {
                    Directory.CreateDirectory(DirectoryPath);
                }

                if (File.Exists(FilePath) == false)
                {
                    var dummy = new Preferences();
                    var dummyJson = JsonConvert.SerializeObject(dummy);
                    File.WriteAllText(FilePath, dummyJson);
                }

                _instance = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(FilePath));
                if (_instance == null)
                {
                    _instance = new Preferences();
                }
            }
        }

        public List<string> RecentProjects { get; set; } = new List<string>();

        public void AddRecentProject(string projectFile)
        {
            RecentProjects.RemoveAll(o => o.ToLower() == projectFile.ToLower());
            RecentProjects.Insert(0, projectFile);
        }

        public void RemoveRecentProject(string projectFile)
        {
            RecentProjects.RemoveAll(o => o.ToLower() == projectFile.ToLower());
        }

        static public void Save()
        {
            EnsureInstanceIsCreated();

            File.WriteAllText(FilePath, JsonConvert.SerializeObject(_instance));
        }

        static public string DirectoryPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Katzebase");
            }
        }

        static public string FilePath
        {
            get
            {
                return Path.Combine(DirectoryPath, "UI.json");
            }
        }

        public int FormStudioWidth { get; set; } = 1200;
        public int FormStudioHeight { get; set; } = 800;
        public int ResultsSplitterDistance { get; set; } = 400;
        public int ObjectExplorerSplitterDistance { get; set; } = 350;
    }
}
