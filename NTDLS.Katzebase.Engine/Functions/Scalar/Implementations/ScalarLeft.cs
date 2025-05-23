﻿using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarLeft
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var text = function.Get<string?>("text");
            if (text == null)
            {
                return null;
            }
            return text.Substring(0, function.Get<int>("length"));
        }
    }
}
