﻿using NTDLS.Helpers;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarFormatNumeric
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var value = function.Get<double?>("value");
            var format = function.Get<string?>("format")?.ToLowerInvariant();

            if (value == null)
            {
                return null;
            }

            return value.EnsureNotNull().ToString(format);
        }
    }
}
