using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    /// <summary>
    /// Contains all function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    internal class AggregateFunctionImplementation
    {
        internal static string[] PrototypeStrings = {
                //Prototype Format: "returnDataType functionName (parameterDataType parameterName, parameterDataType parameterName = defaultValue)"
                //Parameters that have a default specified are considered optional, they should come after non-optional parameters.
                "Numeric Count (AggregationArray values, boolean countDistinct = false)",
                "Numeric Sum (AggregationArray values)",
                "Numeric Min (AggregationArray values)",
                "Numeric Max (AggregationArray values)",
                "Numeric Avg (AggregationArray values)"
            };

        public static string ExecuteFunction(string functionName, List<string> aggregationValues, List<string> supplementalParameters)
        {
            var proc = AggregateFunctionCollection.ApplyFunctionPrototype(functionName, supplementalParameters);

            switch (functionName.ToLowerInvariant())
            {
                case "sum":
                    {
                        return aggregationValues.Sum(o => double.Parse(o)).ToString();
                    }
                case "min":
                    {
                        return aggregationValues.Min(o => double.Parse(o)).ToString();
                    }
                case "max":
                    {
                        return aggregationValues.Max(o => double.Parse(o)).ToString();
                    }
                case "avg":
                    {
                        return aggregationValues.Average(o => double.Parse(o)).ToString();
                    }
                case "count":
                    {
                        if (proc.Get<bool>("countDistinct"))
                        {
                            return aggregationValues.Distinct().Count().ToString();
                        }
                        return aggregationValues.Count().ToString();
                    }
            }

            throw new KbFunctionException($"Undefined function: {functionName}.");
        }
    }
}
