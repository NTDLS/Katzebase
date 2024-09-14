using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Tokens;

namespace NTDLS.Katzebase.Engine.Functions.Scaler
{
    /// <summary>
    /// Contains a parsed function prototype.
    /// </summary>
    public class ScalerFunction
    {
        public string Name { get; private set; }
        public KbScalerFunctionParameterType ReturnType { get; private set; }
        public List<ScalerFunctionParameterPrototype> Parameters { get; private set; } = new();


        public ScalerFunction(string name, KbScalerFunctionParameterType returnType, List<ScalerFunctionParameterPrototype> parameters)
        {
            Name = name;
            ReturnType = returnType;
            Parameters.AddRange(parameters);
        }

        public static ScalerFunction Parse(string prototype)
        {
            var tokenizer = new Tokenizer(prototype, true);

            string token = tokenizer.EatGetNext();

            if (Enum.TryParse(token, true, out KbScalerFunctionParameterType returnType) == false)
            {
                throw new KbEngineException($"Unknown scaler function return type: [{token}].");
            }

            if (tokenizer.TryEatValidateNextToken((o) => TokenizerExtensions.IsIdentifier(o), out var functionName) == false)
            {
                throw new KbEngineException($"Invalid scaler function name: [{functionName}].");
            }

            bool foundOptionalParameter = false;
            bool infiniteParameterFound = false;

            var parameters = new List<ScalerFunctionParameterPrototype>();
            var parametersStrings = tokenizer.EatGetMatchingScope().ScopeSensitiveSplit(',');

            foreach (var parametersString in parametersStrings)
            {
                var paramTokenizer = new Tokenizer(parametersString);

                token = paramTokenizer.EatGetNext();
                if (Enum.TryParse(token, true, out KbScalerFunctionParameterType paramType) == false)
                {
                    throw new KbEngineException($"Unknown scaler function [{functionName}] parameter type: [{token}].");
                }

                if (paramType == KbScalerFunctionParameterType.NumericInfinite || paramType == KbScalerFunctionParameterType.StringInfinite)
                {
                    if (infiniteParameterFound)
                    {
                        throw new KbEngineException($"Invalid scaler function [{functionName}] prototype. Function cannot contain more than one infinite parameter.");
                    }

                    if (foundOptionalParameter)
                    {
                        throw new KbEngineException($"Invalid scaler function [{functionName}] prototype. Function cannot contain both infinite parameters and optional parameters.");
                    }

                    infiniteParameterFound = true;
                }

                if (paramTokenizer.TryEatValidateNextToken((o) => TokenizerExtensions.IsIdentifier(o), out var parameterName) == false)
                {
                    throw new KbEngineException($"Invalid scaler function [{functionName}] parameter name: [{parameterName}].");
                }

                if (!paramTokenizer.IsExhausted()) //Parse optional parameter default value.
                {
                    if (infiniteParameterFound)
                    {
                        throw new KbEngineException($"Invalid scaler function [{functionName}] prototype. Function cannot contain both infinite parameters and optional parameters.");
                    }

                    if (paramTokenizer.TryEatIsNextCharacter('=') == false)
                    {
                        throw new KbEngineException($"Invalid scaler function [{functionName}] prototype when parsing optional parameter [{parameterName}]. Expected '=', found: [{paramTokenizer.NextCharacter}].");
                    }

                    token = paramTokenizer.EatGetNext();

                    var optionalParameterDefaultValue = tokenizer.ResolveLiteral(token);

                    parameters.Add(new ScalerFunctionParameterPrototype(paramType, parameterName, optionalParameterDefaultValue));

                    foundOptionalParameter = true;
                }
                else
                {
                    if (foundOptionalParameter)
                    {
                        //If we have already found an optional parameter, then all remaining parameters must be optional
                        throw new KbEngineException($"Invalid scaler function [{functionName}] parameter [{parameterName}] must define a default.");
                    }

                    parameters.Add(new ScalerFunctionParameterPrototype(paramType, parameterName));
                }

                if (paramType == KbScalerFunctionParameterType.StringInfinite)
                {
                    if (!tokenizer.IsExhausted())
                    {
                        throw new KbEngineException($"Failed to parse scaler function [{functionName}] prototype, infinite parameter [{parameterName}] must be the last parameter.");
                    }
                }
            }

            if (!tokenizer.IsExhausted())
            {
                throw new KbEngineException($"Failed to parse scaler function [{functionName}] prototype, expected end-of-line: [{tokenizer.Remainder}].");
            }

            return new ScalerFunction(functionName, returnType, parameters);
        }

        internal ScalerFunctionParameterValueCollection ApplyParameters(List<string?> values)
        {
            var result = new ScalerFunctionParameterValueCollection();

            int satisfiedParameterCount = 0;

            for (int protoParamIndex = 0; protoParamIndex < Parameters.Count; protoParamIndex++)
            {
                if (Parameters[protoParamIndex].Type == KbScalerFunctionParameterType.StringInfinite)
                {
                    //This is an infinite parameter, and since these are intended to be defined as the last
                    //parameter in the prototype, it eats the remainder of the passed parameters.
                    for (int passedParamIndex = protoParamIndex; passedParamIndex < values.Count; passedParamIndex++)
                    {
                        result.Values.Add(new ScalerFunctionParameterValue(Parameters[protoParamIndex], values[passedParamIndex]));
                    }
                    break;
                }

                if (protoParamIndex > values.Count)
                {
                    if (Parameters[protoParamIndex].HasDefault)
                    {
                        result.Values.Add(new ScalerFunctionParameterValue(Parameters[protoParamIndex], Parameters[protoParamIndex].DefaultValue));
                    }
                    else
                    {
                        throw new KbFunctionException($"Function [{Name}] parameter [{Parameters[protoParamIndex].Name}] passed is not optional.");
                    }
                }
                else
                {
                    result.Values.Add(new ScalerFunctionParameterValue(Parameters[protoParamIndex], values[protoParamIndex]));
                }

                satisfiedParameterCount++;
            }

            if (satisfiedParameterCount != Parameters.Count)
            {
                throw new KbFunctionException($"Incorrect number of parameters passed to [{Name}].");
            }

            return result;
        }
    }
}
