using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Functions.Aggregate.Parameters;
using NTDLS.Katzebase.Engine.Query.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    /// <summary>
    /// Contains all function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    internal class AggregateFunctionImplementation
    {
        internal static string[] PrototypeStrings = {
                "count:NumericArray/fieldName",
                "sum:NumericArray/fieldName",
                "min:NumericArray/fieldName",
                "max:NumericArray/fieldName",
                "avg:NumericArray/fieldName"
            };

        public static string? ExecuteFunction(string functionName, double[] parameters, KbInsensitiveDictionary<List<string>> groupedValues)
        {


            switch (functionName.ToLowerInvariant())
            {
                case "sum":
                    {
                        return parameters.Sum(o => o).ToString();
                    }
                case "min":
                    {
                        return parameters.Min(o => o).ToString();
                    }
                case "max":
                    {
                        return parameters.Max(o => o).ToString();
                    }
                case "avg":
                    {
                        return parameters.Average(o => o).ToString();
                    }
                case "count":
                    {
                        return parameters.Count().ToString();
                    }
            }

            throw new KbFunctionException($"Undefined function: {functionName}.");
        }
    }
}
