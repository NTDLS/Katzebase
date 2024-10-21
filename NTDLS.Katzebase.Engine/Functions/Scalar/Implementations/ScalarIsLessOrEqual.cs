﻿using NTDLS.Katzebase.Parsers.Conditions;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsLessOrEqual
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            return (ConditionEntry.IsMatchLesserOrEqual(function.Get<int?>("value1"), function.Get<int?>("value2")) == true ? 1 : 0).ToString();
        }
    }
}
