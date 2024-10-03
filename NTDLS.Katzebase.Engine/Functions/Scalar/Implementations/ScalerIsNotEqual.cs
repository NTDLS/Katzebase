using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Parsers.Interfaces;

using NTDLS.Katzebase.Parsers.Functions.Scaler;
namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerIsNotEqual
    {
        public static string? Execute<TData>(Transaction<TData> transaction, ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            return (ConditionEntry<TData>.IsMatchEqual<TData>(transaction, function.Get<string>("text1"), function.Get<string>("text2")) == false).ToString();
        }
    }
}
