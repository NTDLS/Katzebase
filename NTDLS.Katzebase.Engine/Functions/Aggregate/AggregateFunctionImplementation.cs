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

        public static string ExecuteFunction(string functionName, List<string> parameters, List<string> groupedValues)
        {
            var proc = AggregateFunctionCollection.ApplyFunctionPrototype(functionName, parameters);

            switch (functionName.ToLowerInvariant())
            {
                case "sum":
                    {
                        return groupedValues.Sum(o => double.Parse(o)).ToString();
                    }
                case "min":
                    {
                        return groupedValues.Min(o => double.Parse(o)).ToString();
                    }
                case "max":
                    {
                        return groupedValues.Max(o => double.Parse(o)).ToString();
                    }
                case "avg":
                    {
                        return groupedValues.Average(o => double.Parse(o)).ToString();
                    }
                case "count":
                    {
                        if (proc.Get<bool>("countDistinct"))
                        {
                            return groupedValues.Distinct().Count().ToString();
                        }
                        return groupedValues.Count().ToString();
                    }
            }

            throw new KbFunctionException($"Undefined function: {functionName}.");
        }
    }
}
