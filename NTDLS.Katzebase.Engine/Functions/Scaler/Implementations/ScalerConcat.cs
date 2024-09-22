using System.Text;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerConcat
    {
        public static string? Execute(List<string?> parameters)
        {
            var builder = new StringBuilder();
            foreach (var p in parameters)
            {
                builder.Append(p);
            }
            return builder.ToString();
        }
    }
}
