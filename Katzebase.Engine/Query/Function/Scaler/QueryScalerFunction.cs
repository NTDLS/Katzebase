using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Query.Function.Scaler
{
    /// <summary>
    /// Contains a parsed function prototype.
    /// </summary>
    internal class QueryScalerFunction
    {
        public string Name { get; set; }
        public List<QueryScalerFunctionParameterPrototype> Parameters { get; private set; } = new();

        public QueryScalerFunction(string name, List<QueryScalerFunctionParameterPrototype> parameters)
        {
            Name = name;
            Parameters.AddRange(parameters);
        }

        public static QueryScalerFunction Parse(string prototype)
        {
            int indexOfMethodNameEnd = prototype.IndexOf(':');
            string methodName = prototype.Substring(0, indexOfMethodNameEnd);
            var parameterStrings = prototype.Substring(indexOfMethodNameEnd + 1).Split(',', StringSplitOptions.RemoveEmptyEntries);
            List<QueryScalerFunctionParameterPrototype> parameters = new();

            foreach (var param in parameterStrings)
            {
                var typeAndName = param.Split("/");
                if (Enum.TryParse(typeAndName[0], true, out KbParameterType paramType) == false)
                {
                    throw new KbGenericException($"Unknown parameter type {typeAndName[0]}");
                }

                var nameAndDefault = typeAndName[1].Trim().Split('=');

                if (nameAndDefault.Count() == 1)
                {
                    parameters.Add(new QueryScalerFunctionParameterPrototype(paramType, nameAndDefault[0]));
                }
                else if (nameAndDefault.Count() == 2)
                {
                    parameters.Add(new QueryScalerFunctionParameterPrototype(paramType, nameAndDefault[0], nameAndDefault[1]));
                }
                else
                {
                    throw new KbGenericException($"Wrong number of default parameters supplied to prototype for {typeAndName[0]}");
                }
            }

            return new QueryScalerFunction(methodName, parameters);
        }

        internal QueryScalerFunctionParameterValueCollection ApplyParameters(List<string?> values)
        {
            int requiredParameterCount = Parameters.Where(o => o.Type.ToString().ToLower().Contains("optional") == false).Count();

            if (Parameters.Count < requiredParameterCount)
            {
                if (Parameters.Count > 0 && Parameters[0].Type == KbParameterType.Infinite_String)
                {
                    //The first parameter is infinite, we dont even check anything else.
                }
                else
                {
                    throw new KbFunctionException($"Incorrect number of parameter passed to {Name}.");
                }
            }

            var result = new QueryScalerFunctionParameterValueCollection();

            if (Parameters.Count > 0 && Parameters[0].Type == KbParameterType.Infinite_String)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    result.Values.Add(new QueryScalerFunctionParameterValue(Parameters[0], values[i]));
                }
            }
            else
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (i >= values.Count)
                    {
                        result.Values.Add(new QueryScalerFunctionParameterValue(Parameters[i]));
                    }
                    else
                    {
                        result.Values.Add(new QueryScalerFunctionParameterValue(Parameters[i], values[i]));
                    }
                }
            }

            return result;
        }
    }
}
