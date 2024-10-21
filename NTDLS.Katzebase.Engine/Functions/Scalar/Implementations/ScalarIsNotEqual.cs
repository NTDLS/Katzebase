using NTDLS.Katzebase.Parsers.Conditions;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsNotEqual
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return (ConditionEntry.IsMatchEqual(function.Get<string?>("text1"), function.Get<string?>("text2")) == false ? 1 : 0).ToString();
        }
    }
}
