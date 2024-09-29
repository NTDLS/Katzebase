namespace NTDLS.Katzebase.Parsers.Functions.Parameters
{
    public static class FunctionParameterTypes
    {
        internal static string[] Old_AggregateFunctionNames = { "min", "max", "sum", "avg", "count" };

        public enum FunctionType
        {
            Aggregate,
            Scaler
        }
    }
}
