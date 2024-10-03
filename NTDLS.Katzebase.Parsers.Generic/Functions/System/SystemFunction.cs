using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Parsers.Functions.System
{
    /// <summary>
    /// Contains a parsed function prototype.
    /// </summary>
    public class SystemFunction<TData> where TData : IStringable
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public List<SystemFunctionParameterPrototype<TData>> Parameters { get; private set; } = new();

        public SystemFunction(string name, List<SystemFunctionParameterPrototype<TData>> parameters, string description)
        {
            Name = name;
            Description = description;
            Parameters.AddRange(parameters);
        }

        public static SystemFunction<TData> Parse(string prototype, Func<string, TData> strParse2TData)
        {
            var tokenizer = new Tokenizer<TData>(prototype, strParse2TData, true);

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var functionName) == false)
            {
                throw new KbEngineException($"Invalid system function name: [{functionName}].");
            }

            bool foundOptionalParameter = false;
            bool infiniteParameterFound = false;

            var parameters = new List<SystemFunctionParameterPrototype<TData>>();
            var parametersStrings = tokenizer.EatGetMatchingScope().ScopeSensitiveSplit(',');

            foreach (var parametersString in parametersStrings)
            {
                var paramTokenizer = new Tokenizer<TData>(parametersString, parse: strParse2TData);

                var paramType = paramTokenizer.EatIfNextEnum<KbSystemFunctionParameterType>();

                if (paramType == KbSystemFunctionParameterType.NumericInfinite || paramType == KbSystemFunctionParameterType.StringInfinite)
                {
                    if (infiniteParameterFound)
                    {
                        throw new KbEngineException($"Invalid system function [{functionName}] prototype. Function cannot contain more than one infinite parameter.");
                    }

                    if (foundOptionalParameter)
                    {
                        throw new KbEngineException($"Invalid system function [{functionName}] prototype. Function cannot contain both infinite parameters and optional parameters.");
                    }

                    infiniteParameterFound = true;
                }

                if (paramTokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var parameterName) == false)
                {
                    throw new KbEngineException($"Invalid system function [{functionName}] parameter name: [{parameterName}].");
                }

                if (!paramTokenizer.IsExhausted()) //Parse optional parameter default value.
                {
                    if (infiniteParameterFound)
                    {
                        throw new KbEngineException($"Invalid system function [{functionName}] prototype. Function cannot contain both infinite parameters and optional parameters.");
                    }

                    if (paramTokenizer.TryEatIfNextCharacter('=') == false)
                    {
                        throw new KbEngineException($"Invalid system function [{functionName}] prototype when parsing optional parameter [{parameterName}]. Expected [=], found: [{paramTokenizer.NextCharacter}].");
                    }

                    var optionalParameterDefaultValue = tokenizer.ResolveLiteral(paramTokenizer.EatGetNext());
                    if (optionalParameterDefaultValue == null || optionalParameterDefaultValue.ToT<string>() == "null")
                    {
                        optionalParameterDefaultValue = default;
                    }

                    parameters.Add(new SystemFunctionParameterPrototype<TData>(paramType, parameterName, optionalParameterDefaultValue));

                    foundOptionalParameter = true;
                }
                else
                {
                    if (foundOptionalParameter)
                    {
                        //If we have already found an optional parameter, then all remaining parameters must be optional
                        throw new KbEngineException($"Invalid system function [{functionName}] parameter [{parameterName}] must define a default.");
                    }

                    parameters.Add(new SystemFunctionParameterPrototype<TData>(paramType, parameterName));
                }

                if (paramType == KbSystemFunctionParameterType.StringInfinite)
                {
                    if (!tokenizer.IsExhausted())
                    {
                        throw new KbEngineException($"Failed to parse system function [{functionName}] prototype, infinite parameter [{parameterName}] must be the last parameter.");
                    }
                }
            }

            string description = string.Empty;
            if (tokenizer.TryEatIfNext('|'))
            {
                description = tokenizer.EatRemainder();
            }

            if (!tokenizer.IsExhausted())
            {
                throw new KbEngineException($"Failed to parse system function [{functionName}] prototype, expected end-of-line: [{tokenizer.Remainder}].");
            }

            return new SystemFunction<TData>(functionName, parameters, description);
        }

        internal SystemFunctionParameterValueCollection<TData> ApplyParameters(List<TData?> values)
        {
            var result = new SystemFunctionParameterValueCollection<TData>();

            int satisfiedParameterCount = 0;

            for (int protoParamIndex = 0; protoParamIndex < Parameters.Count; protoParamIndex++)
            {
                if (Parameters[protoParamIndex].Type == KbSystemFunctionParameterType.StringInfinite)
                {
                    //This is an infinite parameter, and since these are intended to be defined as the last
                    //parameter in the prototype, it eats the remainder of the passed parameters.
                    for (int passedParamIndex = protoParamIndex; passedParamIndex < values.Count; passedParamIndex++)
                    {
                        result.Values.Add(new SystemFunctionParameterValue<TData>(Parameters[protoParamIndex], values[passedParamIndex]));
                    }
                    break;
                }

                if (protoParamIndex >= values.Count)
                {
                    if (Parameters[protoParamIndex].HasDefault)
                    {
                        result.Values.Add(new SystemFunctionParameterValue<TData>(Parameters[protoParamIndex], Parameters[protoParamIndex].DefaultValue));
                    }
                    else
                    {
                        throw new KbFunctionException($"Function [{Name}] parameter [{Parameters[protoParamIndex].Name}] passed is not optional.");
                    }
                }
                else
                {
                    result.Values.Add(new SystemFunctionParameterValue<TData>(Parameters[protoParamIndex], values[protoParamIndex]));
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
