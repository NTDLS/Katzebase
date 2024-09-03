using NTDLS.Katzebase.Client.Exceptions;
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

        private static string? ExecuteFunction(string functionName, List<AggregateGenericParameter> parameters, IGrouping<string, SchemaIntersectionRow> group)
        {
            var proc = AggregateFunctionCollection.ApplyFunctionPrototype(functionName, parameters);

            switch (functionName.ToLowerInvariant())
            {
                case "sum":
                    {
                        var arrayOfValues = proc.Get<AggregateDecimalArrayParameter>("fieldName");
                        return arrayOfValues.Values.Sum(o => o).ToString();
                    }
                case "min":
                    {
                        var arrayOfValues = proc.Get<AggregateDecimalArrayParameter>("fieldName");
                        return arrayOfValues.Values.Min(o => o).ToString();
                    }
                case "max":
                    {
                        var arrayOfValues = proc.Get<AggregateDecimalArrayParameter>("fieldName");
                        return arrayOfValues.Values.Max(o => o).ToString();
                    }
                case "avg":
                    {
                        var arrayOfValues = proc.Get<AggregateDecimalArrayParameter>("fieldName");
                        return arrayOfValues.Values.Average(o => o).ToString();
                    }
                case "count":
                    {
                        var arrayOfValues = proc.Get<AggregateDecimalArrayParameter>("fieldName");
                        return arrayOfValues.Values.Count.ToString();
                    }
            }

            throw new KbFunctionException($"Undefined function: {functionName}.");
        }
    }
}
