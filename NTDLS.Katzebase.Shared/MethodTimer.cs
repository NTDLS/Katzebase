#if DEBUG
using System.Diagnostics;
#endif

namespace NTDLS.Katzebase.Shared
{
    public class MethodTimer
    {
        public delegate void MethodTimerProc();

#if DEBUG
        public static void DebugPrintTime(string friendlyName, MethodTimerProc proc)
        {
            Debug.WriteLine($"Starting: {friendlyName}");
            var startTime = DateTime.UtcNow;
            proc();
            Debug.WriteLine($"Completed: {friendlyName}: {(DateTime.UtcNow - startTime).TotalMilliseconds:n0}");
        }

#else
        public static void DebugPrintTime(string friendlyName, MethodTimerProc proc) => proc();
#endif

        public static double TimeExecution(MethodTimerProc proc)
        {
            var startTime = DateTime.UtcNow;
            proc();
            return (DateTime.UtcNow - startTime).TotalMilliseconds;
        }
    }
}
