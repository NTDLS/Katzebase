using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsNotEqual
    {
        public static string? Execute(Transaction transaction, ScalarFunctionParameterValueCollection function)
        {
            return (ConditionEntry.IsMatchEqual(transaction, function.Get<string>("text1"), function.Get<string>("text2")) == false).ToString();
        }
    }
}
