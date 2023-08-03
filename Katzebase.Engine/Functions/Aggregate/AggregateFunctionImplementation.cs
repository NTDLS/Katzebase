using Katzebase.Engine.Functions.Aggregate.Parameters;
using Katzebase.Engine.Functions.Parameters;
using Katzebase.Engine.Query.Searchers.Intersection;
using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Functions.Aggregate
{
    /// <summary>
    /// Contains all function protype defintions, function implementations and expression collapse functionality.
    /// </summary>
    internal class AggregateFunctionImplementation
    {
        internal static string[] FunctionPrototypes = {
                "count:NumericArray/fieldName",
                "sum:NumericArray/fieldName",
                "min:NumericArray/fieldName",
                "max:NumericArray/fieldName",
                "avg:NumericArray/fieldName"
            };

        internal static string? CollapseAllFunctionParameters(FunctionParameterBase param, IGrouping<string, SchemaIntersectionRow> group)
        {
            if (param is FunctionWithParams)
            {
                var subParams = new List<AggregateGenericParameter>();

                foreach (var subParam in ((FunctionWithParams)param).Parameters)
                {
                    var specificParam = (FunctionDocumentFieldParameter)subParam;
                    var values = group.SelectMany(o => o.AuxiliaryFields.Where(m => m.Key == specificParam.Value.Key)).Select(s => s.Value);
                    subParams.Add(new AggregateDecimalArrayParameter() { Values = values.Select(o => decimal.Parse(o ?? "0")).ToList() });
                }

                return ExecuteFunction(((FunctionWithParams)param).Function, subParams, group);
            }
            else if (param is FunctionConstantParameter)
            {
                return ((FunctionConstantParameter)param).RawValue;
            }
            /*
            else if (param is FunctionDocumentFieldParameter)
            {
                var specificParam = (FunctionDocumentFieldParameter)param;

                var debug = group.Select(o => o.AuxiliaryFields.Where(m => m.Key == specificParam.Value.Key)).ToList();

                var methodValue = group.Select(o => o.AuxiliaryFields.Where(m => m.Key == specificParam.Value.Key)).Single().Select(o => o.Value).Single();
                return methodValue;
            }
            */
            else
            {
                throw new KbNotImplementedException($"The aggregate function type {param.GetType} is not implemented.");
            }

        }

        private static string? ExecuteFunction(string functionName, List<AggregateGenericParameter> parameters, IGrouping<string, SchemaIntersectionRow> group)
        {
            var proc = AggregateFunctionCollection.ApplyFunctionPrototype(functionName, parameters);

            switch (functionName.ToLower())
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
                        return arrayOfValues.Values.Count().ToString();
                    }
            }

            throw new KbFunctionException($"Undefined function: {functionName}.");
        }
    }
}
