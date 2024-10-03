namespace NTDLS.Katzebase.Engine.Functions.Parameters
{
    internal class FunctionConstantParameter : FunctionParameterBase
    {
        public string RawValue { get; set; } = string.Empty;

        /// <summary>
        /// This is the value that should be used for "user code". It removes the quotes from constant parameters.
        /// </summary>
        public string FinalValue
        {
            get
            {
                if (RawValue.StartsWith('\'') && RawValue.EndsWith('\''))
                {
                    return RawValue.Substring(1, RawValue.Length - 2);
                }
                return RawValue;
            }
        }

        public FunctionConstantParameter(string value)
        {
            RawValue = value;
        }
    }
}
