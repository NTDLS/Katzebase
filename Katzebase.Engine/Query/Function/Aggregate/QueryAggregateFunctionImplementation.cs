using Katzebase.Engine.Query.FunctionParameter;
using Katzebase.Engine.Query.Searchers;
using Katzebase.Engine.Query.Searchers.Intersection;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using System.Linq;
using System.Web;

namespace Katzebase.Engine.Query.Function.Aggregate
{
    /// <summary>
    /// Contains all function protype defintions, function implementations and expression collapse functionality.
    /// </summary>
    internal class QueryAggregateFunctionImplementation
    {
        internal static string[] FunctionPrototypes = {
                "count:string/fieldName",
                "sum:string/fieldName",
                "min:string/fieldName",
                "max:string/fieldName",
                "avg:string/fieldName",
            };

        internal class ArrayParameter
        {
            public List<decimal> Values { get; set; } = new();
        }

        internal static string? CollapseAllFunctionParameters(FunctionParameterBase param, IGrouping<string, SchemaIntersectionRow> group)
        {

            if (param is FunctionWithParams)
            {
                var subParams = new List<ArrayParameter>();

                foreach (var subParam in ((FunctionWithParams)param).Parameters)
                {
                    var specificParam = (FunctionDocumentFieldParameter)subParam;
                    var values = group.SelectMany(o => o.MethodFields.Where(m => m.Key == specificParam.Value.Key)).Select(s => s.Value);
                    subParams.Add(new ArrayParameter() { Values = values.Select(o => decimal.Parse(o ?? "0")).ToList() });
                }

                return ExecuteFunction(((FunctionWithParams)param).Function, subParams, group);
            }
            else if (param is FunctionConstantParameter)
            {
                return ((FunctionConstantParameter)param).Value;
            }
            /*
            else if (param is FunctionDocumentFieldParameter)
            {
                var specificParam = (FunctionDocumentFieldParameter)param;

                var debug = group.Select(o => o.MethodFields.Where(m => m.Key == specificParam.Value.Key)).ToList();

                var methodValue = group.Select(o => o.MethodFields.Where(m => m.Key == specificParam.Value.Key)).Single().Select(o => o.Value).Single();
                return methodValue;
            }
            */
            else
            {
                throw new KbNotImplementedException($"The aggregate function type {param.GetType} is not implemented.");
            }

        }

        private static string? ExecuteFunction(string functionName, List<ArrayParameter> parameters, IGrouping<string, SchemaIntersectionRow> group)
        {
            //var proc = QueryAggregateFunctionCollection.ApplyFunctionPrototype(functionName, parameters);

            switch (functionName.ToLower())
            {
                case "sum":
                    {
                        return parameters.First().Values.Sum(o => o).ToString();
                    }
                case "min":
                    {
                        return parameters.First().Values.Min(o => o).ToString();
                    }
                case "max":
                    {
                        return parameters.First().Values.Max(o => o).ToString();
                    }
                case "avg":
                    {
                        return parameters.First().Values.Average(o => o).ToString();
                    }
                case "count":
                    {
                        return parameters.First().Values.Count().ToString();
                    }
            }

            throw new KbFunctionException($"Undefined function: {functionName}.");
        }
    }
}
