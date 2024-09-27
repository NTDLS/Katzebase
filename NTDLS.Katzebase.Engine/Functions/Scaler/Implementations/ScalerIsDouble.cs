﻿namespace NTDLS.Katzebase.Engine.Functions.Scaler.Implementations
{
    internal static class ScalerIsDouble
    {
        public static string? Execute(ScalerFunctionParameterValueCollection function)
        {
            return double.TryParse(function.Get<string>("value"), out _).ToString();
        }
    }
}
