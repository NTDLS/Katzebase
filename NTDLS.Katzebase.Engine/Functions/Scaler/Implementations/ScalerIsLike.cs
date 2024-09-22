using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIsLike
    {
        public static string? Execute(Transaction transaction, ScalerFunctionParameterValueCollection function)
        {
            return (ConditionEntry.IsMatchLike(transaction, function.Get<string>("text"), function.Get<string>("pattern")) == true).ToString();
        }
    }
}
