using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Query.Function.Aggregate
{
    /// <summary>
    /// Contains a parsed function prototype.
    /// </summary>
    internal class QueryAggregateFunction
    {
        public string Name { get; set; }
        public List<QueryAggregateFunctionParameterPrototype> Parameters { get; private set; } = new();

        public QueryAggregateFunction(string name, List<QueryAggregateFunctionParameterPrototype> parameters)
        {
            Name = name;
            Parameters.AddRange(parameters);
        }

        public static QueryAggregateFunction Parse(string prototype)
        {
            int indexOfNameEnd = prototype.IndexOf(':');
            string functionName = prototype.Substring(0, indexOfNameEnd);
            var parameterStrings = prototype.Substring(indexOfNameEnd + 1).Split(',', StringSplitOptions.RemoveEmptyEntries);
            List<QueryAggregateFunctionParameterPrototype> parameters = new();

            foreach (var param in parameterStrings)
            {
                var typeAndName = param.Split("/");
                if (Enum.TryParse(typeAndName[0], true, out KbQueryAggregateFunctionParameterType paramType) == false)
                {
                    throw new KbGenericException($"Unknown parameter type {typeAndName[0]}");
                }

                var nameAndDefault = typeAndName[1].Trim().Split('=');

                if (nameAndDefault.Count() == 1)
                {
                    parameters.Add(new QueryAggregateFunctionParameterPrototype(paramType, nameAndDefault[0]));
                }
                else if (nameAndDefault.Count() == 2)
                {
                    parameters.Add(new QueryAggregateFunctionParameterPrototype(paramType, nameAndDefault[0],
                        nameAndDefault[1].ToLower() == "null" ? null : nameAndDefault[1]));
                }
                else
                {
                    throw new KbGenericException($"Wrong number of default parameters supplied to prototype for {typeAndName[0]}");
                }
            }

            return new QueryAggregateFunction(functionName, parameters);
        }

        internal QueryAggregateFunctionParameterValueCollection ApplyParameters(List<string?> values)
        {
            int requiredParameterCount = Parameters.Where(o => o.Type.ToString().ToLower().Contains("optional") == false).Count();

            if (Parameters.Count < requiredParameterCount)
            {
                if (Parameters.Count > 0 && Parameters[0].Type == KbQueryAggregateFunctionParameterType.Infinite_String)
                {
                    //The first parameter is infinite, we dont even check anything else.
                }
                else
                {
                    throw new KbFunctionException($"Incorrect number of parameter passed to {Name}.");
                }
            }

            var result = new QueryAggregateFunctionParameterValueCollection();

            if (Parameters.Count > 0 && Parameters[0].Type == KbQueryAggregateFunctionParameterType.Infinite_String)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    result.Values.Add(new QueryAggregateFunctionParameterValue(Parameters[0], values[i]));
                }
            }
            else
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (i >= values.Count)
                    {
                        result.Values.Add(new QueryAggregateFunctionParameterValue(Parameters[i]));
                    }
                    else
                    {
                        result.Values.Add(new QueryAggregateFunctionParameterValue(Parameters[i], values[i]));
                    }
                }
            }

            return result;
        }
    }
}
