namespace NTDLS.Katzebase.Engine.Functions.Parameters
{
    internal static class FunctionParameterTypes
    {
        internal static string[] AggregateFunctionNames = { "min", "max", "sum", "avg", "count" };

        internal enum FunctionType
        {
            Aggregate,
            Scaler
        }
    }
}
