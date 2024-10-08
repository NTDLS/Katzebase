﻿using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsBetween
    {
        public static string? Execute(Transaction transaction, ScalarFunctionParameterValueCollection function)
        {
            return (ConditionEntry.IsMatchBetween(transaction, function.Get<int>("value"), function.Get<int>("rangeLow"), function.Get<int>("rangeHigh")) == true).ToString();
        }
    }
}
