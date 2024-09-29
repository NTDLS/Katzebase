using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIsNotLike
    {
        public static string? Execute(Transaction transaction, ScalerFunctionParameterValueCollection function)
        {
            return (ConditionEntry.IsMatchLike(transaction, function.Get<string>("text"), function.Get<string>("pattern")) == false).ToString();
        }
    }
}
