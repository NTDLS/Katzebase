using fs;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerCoalesce
    {
        public static fstring? Execute(List<fstring?> parameters)
        {
            foreach (var p in parameters)
            {
                if (p != null)
                {
                    return p;
                }
            }
            return null;
        }
    }
}
