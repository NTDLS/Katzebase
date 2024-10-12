﻿using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.Scalar;
using NTDLS.Katzebase.Parsers.Query.Conditions;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsGreater
    {
        public static string? Execute(Transaction transaction, ScalarFunctionParameterValueCollection function)
        {
            return (ConditionEntry.IsMatchGreater(transaction, function.Get<int?>("value1"), function.Get<int?>("value2")) == true).ToString();
        }
    }
}
