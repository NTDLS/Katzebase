﻿namespace NTDLS.Katzebase.Parsers.Functions.Scaler
{
    public class ScalerFunctionParameterValue
    {
        public ScalerFunctionParameterPrototype Parameter { get; private set; }
        public string? Value { get; private set; } = null;

        public ScalerFunctionParameterValue(ScalerFunctionParameterPrototype parameter, string? value)
        {
            Parameter = parameter;
            Value = value;
        }

        public ScalerFunctionParameterValue(ScalerFunctionParameterPrototype parameter)
        {
            Parameter = parameter;
        }
    }
}