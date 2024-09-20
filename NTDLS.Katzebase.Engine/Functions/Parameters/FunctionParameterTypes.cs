namespace NTDLS.Katzebase.Engine.Functions.Parameters
{
    internal static class FunctionParameterTypes
    {
        internal static string[] Old_AggregateFunctionNames = { "min", "max", "sum", "avg", "count" };

        internal enum FunctionType
        {
            Aggregate,
            Scaler
        }
    }
}
