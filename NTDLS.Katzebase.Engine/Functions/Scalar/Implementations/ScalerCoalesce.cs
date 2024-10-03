using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerCoalesce
    {
        public static TData? Execute<TData>(List<TData?> parameters) where TData : IStringable
        {
            foreach (var p in parameters)
            {
                if (p != null)
                {
                    return p;
                }
            }
            return default(TData);
        }
    }
}
