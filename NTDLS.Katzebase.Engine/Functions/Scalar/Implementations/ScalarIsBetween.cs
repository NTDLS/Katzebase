using NTDLS.Katzebase.Parsers.Conditions;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsBetween
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return (ConditionEntry.IsMatchBetween(
                function.Get<string?>("value"),
                function.Get<string?>("rangeLow"),
                function.Get<string?>("rangeHigh")) == true ? 1 : 0).ToString();
        }
    }
}
