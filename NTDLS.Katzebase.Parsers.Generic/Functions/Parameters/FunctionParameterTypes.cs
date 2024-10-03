namespace NTDLS.Katzebase.Engine.Functions.Parameters
{
    public static class FunctionParameterTypes
    {
        public static string[] Old_AggregateFunctionNames = { "min", "max", "sum", "avg", "count" };

        internal enum FunctionType
        {
            Aggregate,
            Scaler
        }
    }
}
