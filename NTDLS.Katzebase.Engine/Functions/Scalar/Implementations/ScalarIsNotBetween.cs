using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Query.Conditions;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsNotBetween
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return (ConditionEntry.IsMatchBetween(
                function.Get<int?>("value"),
                function.Get<int?>("rangeLow"),
                function.Get<int?>("rangeHigh")) == false ? 1 : 0).ToString();
        }
    }
}
