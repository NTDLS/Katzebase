﻿using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    public static class ScalerLastIndexOf
    {
        public static string? Execute<TData>(ScalerFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            int startIndex = function.Get<int>("offset");
            if (startIndex > 0)
            {
                return function.Get<string>("textToSearch").LastIndexOf(function.Get<string>("textToFind"), startIndex, StringComparison.InvariantCultureIgnoreCase).ToString();
            }
            return function.Get<string>("textToSearch").LastIndexOf(function.Get<string>("textToFind"), startIndex, StringComparison.InvariantCultureIgnoreCase).ToString();
        }
    }
}
