namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerCoalesce
    {
        public static string? Execute(List<string?> parameters)
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
