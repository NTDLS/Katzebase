namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarCoalesce
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
