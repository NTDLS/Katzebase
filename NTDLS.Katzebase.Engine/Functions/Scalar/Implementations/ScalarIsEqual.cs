using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Query.Conditions;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsEqual
    {
        public static string? Execute(Transaction transaction, ScalarFunctionParameterValueCollection function)
        {
            return (ConditionEntry.IsMatchEqual(transaction, function.Get<string>("text1"), function.Get<string>("text2")) == true).ToString();
        }
    }
}
