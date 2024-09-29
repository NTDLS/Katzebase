using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Management.Classes;
using NTDLS.Persistence;

namespace NTDLS.Katzebase.Management
{
    internal static class Program
    {
        public static ManagementSettings Settings { get; set; } = new ManagementSettings();

        [STAThread]
        static void Main(string[] arg)
        {
            ApplicationConfiguration.Initialize();

            Settings = LocalUserApplicationData.LoadFromDisk($"{KbConstants.FriendlyName}\\Management", new ManagementSettings());

            if (arg.Length == 0)
            {
                Application.Run(new FormStudio());
            }
            else if (arg.Length == 1)
            {
                Application.Run(new FormStudio(arg[0]));
            }

            LocalUserApplicationData.SaveToDisk($"{KbConstants.FriendlyName}\\Management", Settings);

            Preferences.Save();
        }
    }
}
