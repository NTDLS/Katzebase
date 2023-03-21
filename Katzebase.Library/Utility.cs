using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Katzebase.Library
{
    public static class Utility
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            return (new StackTrace()).GetFrame(1).GetMethod().Name;
        }
    }
}
