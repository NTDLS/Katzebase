using NTDLS.Katzebase.Management.Classes.Editor;

namespace NTDLS.Katzebase.Management.Classes
{
    internal static class GlobalState
    {
        public static List<AutoCompleteFunction> AutoCompleteFunctions { get; set; } = new();
    }
}
