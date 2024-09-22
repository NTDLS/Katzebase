using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIsBetween
    {
        public static string? Execute(Transaction transaction, ScalerFunctionParameterValueCollection function)
        {
            return (ConditionEntry.IsMatchBetween(transaction, function.Get<int>("value"), function.Get<int>("rangeLow"), function.Get<int>("rangeHigh")) == true).ToString();
        }
    }
}
