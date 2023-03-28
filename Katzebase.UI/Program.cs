using Katzebase.UI.Classes;

namespace Katzebase.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] arg)
        {
            /*
            Application.ThreadException += Application_ThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
            {
            }
            void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
            {
            }
            */

            ApplicationConfiguration.Initialize();

            if (arg.Length == 0)
            {
                Application.Run(new FormStudio());
            }
            else if (arg.Length == 2)
            {
                if (arg[0].ToLower() == "open")
                {
                    Application.Run(new FormStudio(arg[1]));
                }
                else if (arg[0].ToLower() == "run")
                {
                    Application.Run(new FormExecute(arg[1]));
                }
            }

            Preferences.Save();
        }
    }
}
