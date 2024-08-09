using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Functions.Aggregate.Parameters;
using NTDLS.Katzebase.Engine.Functions.Parameters;
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

        internal static string? CollapseAllFunctionParameters(FunctionParameterBase param, IGrouping<string, SchemaIntersectionRow> group)
        {
            if (param is FunctionWithParams functionWithParams)
            {
                var subParams = new List<AggregateGenericParameter>();

                foreach (var subParam in functionWithParams.Parameters)
                {
                    var specificParam = (FunctionDocumentFieldParameter)subParam;
                    var values = group.SelectMany(o => o.AuxiliaryFields.Where(m => m.Key == specificParam.Value.Key)).Select(s => s.Value);
                    subParams.Add(new AggregateDecimalArrayParameter() { Values = values.Select(o => decimal.Parse(o ?? "0")).ToList() });
                }

                return ExecuteFunction(functionWithParams.Function, subParams, group);
            }
            else if (param is FunctionConstantParameter functionConstantParameter)
            {
                return functionConstantParameter.RawValue;
            }
            /*
            else if (param is FunctionDocumentFieldParameter functionDocumentFieldParameter)
            {
                var debug = group.Select(o => o.AuxiliaryFields.Where(m => m.Key == functionDocumentFieldParameter.Value.Key)).ToList();

                var methodValue = group.Select(o => o.AuxiliaryFields.Where(m => m.Key == functionDocumentFieldParameter.Value.Key)).Single().Select(o => o.Value).Single();
                return methodValue;
            }
            */
            else
            {
                throw new KbNotImplementedException($"Aggregate function type {param.GetType} is not implemented.");
            }

        }

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
