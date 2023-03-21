using Katzebase.Service.Properties;
using System;

namespace Katzebase.Service
{
    public static class Program
    {
        public static Engine.Core Core;

        static void Main(string[] args)
        {
            //System.Diagnostics.Process.Start("CMD.exe", "/C rd C:\\Katzebase /S /Q");

            Library.Settings settings = new Library.Settings()
            {
                BaseAddress = Settings.Default.BaseAddress,
                DataRootPath = Settings.Default.DataRootPath.TrimEnd(new char[] { '/', '\\' }),
                TransactionDataPath = Settings.Default.TransactionDataPath.TrimEnd(new char[] { '/', '\\' }),
                LogDirectory = Settings.Default.LogDirectory.TrimEnd(new char[] { '/', '\\' }),
                FlushLog = Settings.Default.FlushLog,
                AllowIOCaching = Settings.Default.AllowIOCaching,
                AllowDeferredIO = Settings.Default.AllowDeferredIO,
                WriteTraceData = Settings.Default.WriteTraceData,
                CacheScavengeBuffer = Settings.Default.CacheScavengeBuffer,
                CacheScavengeRate = Settings.Default.CacheScavengeRate,
                MaxCacheMemory = Settings.Default.MaxCacheMemory,
                RecordInstanceHealth = Settings.Default.RecordInstanceHealth
            };

            Core = new Engine.Core(settings);

            Core.Start();

            var owinServices = new OWIN.Services();
            owinServices.Start(settings.BaseAddress);

            Core.Log.Write($"Listening on {settings.BaseAddress}.");

            Console.ReadLine(); //Continue running.

            Core.Shutdown();
        }
    }
}
