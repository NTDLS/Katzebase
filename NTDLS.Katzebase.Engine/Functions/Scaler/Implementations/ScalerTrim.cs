using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerTrim
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            var characters = function.GetNullable<string?>("characters");
            if (characters != null)
            {
                return function.Get<string>("text").Trim(characters.ToCharArray());
            }

            return function.Get<string>("text").Trim();
        }
    }
}
