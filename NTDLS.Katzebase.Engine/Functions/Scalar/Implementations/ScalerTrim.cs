﻿using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerTrim
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            var characters = function.GetNullable<string?>("characters");
            if (characters != null)
            {
                return function.Get<string>("text").Trim(characters.ToCharArray());
            }

            return function.Get<string>("text").Trim();
        }
    }
}
