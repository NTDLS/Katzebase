using NTDLS.Katzebase.UI.Classes;
using NTDLS.Persistence;

namespace NTDLS.Katzebase.UI
{
    internal static class Program
    {
        public static UISettings Settings { get; set; } = new UISettings();

        [STAThread]
        static void Main(string[] arg)
        {
            ApplicationConfiguration.Initialize();

            Settings = LocalUserApplicationData.LoadFromDisk("Katzebase\\UI", new UISettings());

            if (arg.Length == 0)
            {
                Application.Run(new FormStudio());
            }
            else if (arg.Length == 1)
            {
                Application.Run(new FormStudio(arg[0]));
            }

            LocalUserApplicationData.SaveToDisk("Katzebase\\UI", Settings);

            Preferences.Save();
        }
    }
}
