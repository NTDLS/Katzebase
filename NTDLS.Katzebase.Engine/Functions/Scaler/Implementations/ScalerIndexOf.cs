using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIndexOf
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return function.Get<string>("textToSearch").IndexOf(
                function.Get<string>("textToFind"), function.Get<int>("offset"), StringComparison.InvariantCultureIgnoreCase).ToString();
        }
    }
}
