using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerIsBetween
    {
        public static string? Execute<TData>(Transaction<TData> transaction, ScalerFunctionParameterValueCollection<TData> function) where TData:IStringable
        {
            return (ConditionEntry<TData>.IsMatchBetween(transaction, function.Get<int>("value"), function.Get<int>("rangeLow"), function.Get<int>("rangeHigh")) == true).ToString();
        }
    }
}
