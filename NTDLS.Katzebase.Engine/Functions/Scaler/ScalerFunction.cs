using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.Functions.Scaler
{
    /// <summary>
    /// Contains a parsed function prototype.
    /// </summary>
    internal class ScalerFunction
    {
        public string Name { get; private set; }
        public KbScalerFunctionParameterType ReturnType { get; private set; }
        public List<ScalerFunctionParameterPrototype> Parameters { get; private set; } = new();

        public ScalerFunction(string name, string returnTypeName, List<ScalerFunctionParameterPrototype> parameters)
        {
            Name = name;

            if (Enum.TryParse(returnTypeName, true, out KbScalerFunctionParameterType returnType) == false)
            {
                throw new KbGenericException($"Unknown return type type {returnTypeName}");
            }

            ReturnType = returnType;
            Parameters.AddRange(parameters);
        }

        public ScalerFunction(string name, KbScalerFunctionParameterType returnType, List<ScalerFunctionParameterPrototype> parameters)
        {
            Name = name;
            ReturnType = returnType;
            Parameters.AddRange(parameters);
        }

        public static ScalerFunction Parse(string prototype)
        {
            int indexOfNameEnd = prototype.IndexOf(':');
            string functionName = prototype.Substring(0, indexOfNameEnd);

            int indexOfReturnTypeEnd = prototype.IndexOf(':', indexOfNameEnd + 1);
            string returnType = prototype.Substring(indexOfNameEnd + 1, (indexOfReturnTypeEnd - indexOfNameEnd) - 1);

            var parameterStrings = prototype.Substring(indexOfReturnTypeEnd + 1).Split(',', StringSplitOptions.RemoveEmptyEntries);
            List<ScalerFunctionParameterPrototype> parameters = new();

            foreach (var param in parameterStrings)
            {
                var typeAndName = param.Split("/");
                if (Enum.TryParse(typeAndName[0], true, out KbScalerFunctionParameterType paramType) == false)
                {
                    throw new KbGenericException($"Unknown parameter type {typeAndName[0]}");
                }

                var nameAndDefault = typeAndName[1].Trim().Split('=');

                if (nameAndDefault.Count() == 1)
                {
                    parameters.Add(new ScalerFunctionParameterPrototype(paramType, nameAndDefault[0]));
                }
                else if (nameAndDefault.Count() == 2)
                {
                    parameters.Add(new ScalerFunctionParameterPrototype(paramType, nameAndDefault[0],
                        nameAndDefault[1].ToLower() == "null" ? null : nameAndDefault[1]));
                }
                else
                {
                    throw new KbGenericException($"Wrong number of default parameters supplied to prototype for {typeAndName[0]}");
                }
            }

            return new ScalerFunction(functionName, returnType, parameters);
        }

        internal ScalerFunctionParameterValueCollection ApplyParameters(List<string?> values)
        {
            int requiredParameterCount = Parameters.Where(o => o.Type.ToString().ToLower().Contains("optional") == false).Count();

            if (Parameters.Count < requiredParameterCount)
            {
                if (Parameters.Count > 0 && Parameters[0].Type == KbScalerFunctionParameterType.Infinite_String)
                {
                    //The first parameter is infinite, we don't even check anything else.
                }
                else
                {
                    throw new KbFunctionException($"Incorrect number of parameter passed to {Name}.");
                }
            }

            var result = new ScalerFunctionParameterValueCollection();

            if (Parameters.Count > 0 && Parameters[0].Type == KbScalerFunctionParameterType.Infinite_String)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    result.Values.Add(new ScalerFunctionParameterValue(Parameters[0], values[i]));
                }
            }
            else
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (i >= values.Count)
                    {
                        result.Values.Add(new ScalerFunctionParameterValue(Parameters[i]));
                    }
                    else
                    {
                        result.Values.Add(new ScalerFunctionParameterValue(Parameters[i], values[i]));
                    }
                }
            }

            return result;
        }
    }
}
