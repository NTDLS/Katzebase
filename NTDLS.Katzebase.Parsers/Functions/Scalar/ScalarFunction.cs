using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Functions.Scalar
{
    /// <summary>
    /// Contains a parsed function prototype.
    /// </summary>
    public class ScalarFunction
    {
        public string Name { get; private set; }
        public string Description { get; private set; }

        public KbScalarFunctionParameterType ReturnType { get; private set; }
        public List<ScalarFunctionParameterPrototype> Parameters { get; private set; } = new();


        public ScalarFunction(string name, KbScalarFunctionParameterType returnType, List<ScalarFunctionParameterPrototype> parameters, string description)
        {
            Name = name;
            Description = description;
            ReturnType = returnType;
            Parameters.AddRange(parameters);
        }

        public static ScalarFunction Parse(string prototype)
        {
            var tokenizer = new Tokenizer(prototype, true);

            var returnType = tokenizer.EatIfNextEnum<KbScalarFunctionParameterType>();

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var functionName) == false)
            {
                throw new KbEngineException($"Invalid scalar function name: [{functionName}].");
            }

            bool foundOptionalParameter = false;
            bool infiniteParameterFound = false;

            var parameters = new List<ScalarFunctionParameterPrototype>();
            var parametersStrings = tokenizer.EatGetMatchingScope().ScopeSensitiveSplit(',');

            foreach (var parametersString in parametersStrings)
            {
                var paramTokenizer = new Tokenizer(parametersString);

                var paramType = paramTokenizer.EatIfNextEnum<KbScalarFunctionParameterType>();

                if (paramType == KbScalarFunctionParameterType.NumericInfinite || paramType == KbScalarFunctionParameterType.StringInfinite)
                {
                    if (infiniteParameterFound)
                    {
                        throw new KbEngineException($"Invalid scalar function [{functionName}] prototype. Function cannot contain more than one infinite parameter.");
                    }

                    if (foundOptionalParameter)
                    {
                        throw new KbEngineException($"Invalid scalar function [{functionName}] prototype. Function cannot contain both infinite parameters and optional parameters.");
                    }

                    infiniteParameterFound = true;
                }

                if (paramTokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var parameterName) == false)
                {
                    throw new KbEngineException($"Invalid scalar function [{functionName}] parameter name: [{parameterName}].");
                }

                if (!paramTokenizer.IsExhausted()) //Parse optional parameter default value.
                {
                    if (infiniteParameterFound)
                    {
                        throw new KbEngineException($"Invalid scalar function [{functionName}] prototype. Function cannot contain both infinite parameters and optional parameters.");
                    }

                    if (paramTokenizer.TryEatIfNextCharacter('=') == false)
                    {
                        throw new KbEngineException($"Invalid scalar function [{functionName}] prototype when parsing optional parameter [{parameterName}]. Expected [=], found: [{paramTokenizer.NextCharacter}].");
                    }

                    var optionalParameterDefaultValue = tokenizer.ResolveLiteral(paramTokenizer.EatGetNext());
                    if (optionalParameterDefaultValue == null || optionalParameterDefaultValue?.Is("null") == true)
                    {
                        optionalParameterDefaultValue = null;
                    }

                    parameters.Add(new ScalarFunctionParameterPrototype(paramType, parameterName, optionalParameterDefaultValue));

                    foundOptionalParameter = true;
                }
                else
                {
                    if (foundOptionalParameter)
                    {
                        //If we have already found an optional parameter, then all remaining parameters must be optional
                        throw new KbEngineException($"Invalid scalar function [{functionName}] parameter [{parameterName}] must define a default.");
                    }

                    parameters.Add(new ScalarFunctionParameterPrototype(paramType, parameterName));
                }

                if (paramType == KbScalarFunctionParameterType.StringInfinite)
                {
                    if (!paramTokenizer.IsExhausted())
                    {
                        throw new KbEngineException($"Failed to parse scalar function [{functionName}] prototype, infinite parameter [{parameterName}] must be the last parameter.");
                    }
                }
            }

            string description = string.Empty;
            if (tokenizer.TryEatIfNext('|'))
            {
                description = tokenizer.EatGetNextEvaluated() ?? string.Empty;
            }

            if (!tokenizer.IsExhausted())
            {
                throw new KbEngineException($"Failed to parse scalar function [{functionName}] prototype, expected end-of-line: [{tokenizer.Remainder}].");
            }

            return new ScalarFunction(functionName, returnType, parameters, description);
        }

        internal ScalarFunctionParameterValueCollection ApplyParameters(List<string?> values)
        {
            var result = new ScalarFunctionParameterValueCollection();

            int satisfiedParameterCount = 0;

            for (int protoParamIndex = 0; protoParamIndex < Parameters.Count; protoParamIndex++)
            {
                if (Parameters[protoParamIndex].Type == KbScalarFunctionParameterType.StringInfinite)
                {
                    satisfiedParameterCount++;

                    //This is an infinite parameter, and since these are intended to be defined as the last
                    //parameter in the prototype, it eats the remainder of the passed parameters.
                    for (int passedParamIndex = protoParamIndex; passedParamIndex < values.Count; passedParamIndex++)
                    {
                        result.Values.Add(new ScalarFunctionParameterValue(Parameters[protoParamIndex], values[passedParamIndex]));
                    }
                    break;
                }

                if (protoParamIndex >= values.Count)
                {
                    if (Parameters[protoParamIndex].HasDefault)
                    {
                        result.Values.Add(new ScalarFunctionParameterValue(Parameters[protoParamIndex], Parameters[protoParamIndex].DefaultValue));
                    }
                    else
                    {
                        throw new KbFunctionException($"Function [{Name}] parameter [{Parameters[protoParamIndex].Name}] passed is not optional.");
                    }
                }
                else
                {
                    result.Values.Add(new ScalarFunctionParameterValue(Parameters[protoParamIndex], values[protoParamIndex]));
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
