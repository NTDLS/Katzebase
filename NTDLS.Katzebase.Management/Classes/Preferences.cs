using Newtonsoft.Json;
using NTDLS.Helpers;

namespace NTDLS.Katzebase.Management.Classes
{
    /// <summary>
    /// Used to store "non-settings" preferences, things like recent files, heights, widths, etc.
    /// </summary>
    internal class Preferences
    {
        static private Preferences? _instance = null;
        static public Preferences Instance
        {
            get
            {
                EnsureInstanceIsCreated();
                return _instance.EnsureNotNull();
            }
        }

        public int FormStudioWidth { get; set; } = 1200;
        public int FormStudioHeight { get; set; } = 800;
        public int ObjectExplorerSplitterDistance { get; set; } = 350;
        public List<string> RecentFiles { get; set; } = new();

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

                _instance = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(FilePath))
                    ?? new Preferences();
            }
        }

        public void AddRecentFile(string projectFile)
        {
            RecentFiles.RemoveAll(o => o.Equals(projectFile, StringComparison.InvariantCultureIgnoreCase));
            RecentFiles.Insert(0, projectFile);
            RecentFiles = RecentFiles.Take(15).ToList();
        }

        public void RemoveRecentFile(string projectFile)
        {
            RecentFiles.RemoveAll(o => o.Equals(projectFile, StringComparison.InvariantCultureIgnoreCase));
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
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Api.KbConstants.FriendlyName);
            }
        }

        static public string FilePath
        {
            get
            {
                return Path.Combine(DirectoryPath, "UI.json");
            }
        }
    }
}
