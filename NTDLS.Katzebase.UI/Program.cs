using NTDLS.Katzebase.UI.Classes;

namespace NTDLS.Katzebase.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] arg)
        {
            ApplicationConfiguration.Initialize();

            if (arg.Length == 0)
            {
                Application.Run(new FormStudio());
            }
            else if (arg.Length == 1)
            {
                Application.Run(new FormStudio(arg[0]));
            }

            Preferences.Save();
        }
    }
}
