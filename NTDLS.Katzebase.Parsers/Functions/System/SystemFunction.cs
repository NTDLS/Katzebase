﻿using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Functions.System
{
    /// <summary>
    /// Contains a parsed function prototype.
    /// </summary>
    public class SystemFunction
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool RequiresAdministrator { get; set; }

        public List<SystemFunctionParameterPrototype> Parameters { get; private set; } = new();

        public SystemFunction(string name, List<SystemFunctionParameterPrototype> parameters, bool requiresAdministrator, string description)
        {
            Name = name;
            Description = description;
            RequiresAdministrator = requiresAdministrator;
            Parameters.AddRange(parameters);
        }

        public static SystemFunction Parse(string prototype)
        {
            var tokenizer = new Tokenizer(prototype, true);

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var functionName) == false)
            {
                throw new KbEngineException($"Invalid system function name: [{functionName}].");
            }

            bool foundOptionalParameter = false;
            bool infiniteParameterFound = false;

            var parameters = new List<SystemFunctionParameterPrototype>();
            var parametersStrings = tokenizer.EatGetMatchingScope().ScopeSensitiveSplit(',');

            foreach (var parametersString in parametersStrings)
            {
                var paramTokenizer = new Tokenizer(parametersString);

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

                    var optionalParameterDefaultValue = tokenizer.Variables.Resolve(paramTokenizer.EatGetNext());
                    if (optionalParameterDefaultValue == null || optionalParameterDefaultValue?.Is("null") == true)
                    {
                        optionalParameterDefaultValue = null;
                    }

                    parameters.Add(new SystemFunctionParameterPrototype(paramType, parameterName, optionalParameterDefaultValue));

                    foundOptionalParameter = true;
                }
                else
                {
                    if (foundOptionalParameter)
                    {
                        //If we have already found an optional parameter, then all remaining parameters must be optional
                        throw new KbEngineException($"Invalid system function [{functionName}] parameter [{parameterName}] must define a default.");
                    }

                    parameters.Add(new SystemFunctionParameterPrototype(paramType, parameterName));
                }

                if (paramType == KbSystemFunctionParameterType.StringInfinite)
                {
                    if (!paramTokenizer.IsExhausted())
                    {
                        throw new KbEngineException($"Failed to parse system function [{functionName}] prototype, infinite parameter [{parameterName}] must be the last parameter.");
                    }
                }
            }

            tokenizer.EatIfNext('|');

            var requiresAdministrator = NTDLS.Helpers.Converters.ConvertTo<bool>(tokenizer.EatGetNext());

            tokenizer.EatIfNext('|');
            var description = tokenizer.EatGetNextResolved() ?? string.Empty;


            if (!tokenizer.IsExhausted())
            {
                throw new KbEngineException($"Failed to parse system function [{functionName}] prototype, expected end-of-line: [{tokenizer.Remainder}].");
            }

            return new SystemFunction(functionName, parameters, requiresAdministrator, description);
        }

        internal SystemFunctionParameterValueCollection ApplyParameters(List<string?> values)
        {
            var result = new SystemFunctionParameterValueCollection();

            int satisfiedParameterCount = 0;

            for (int protoParamIndex = 0; protoParamIndex < Parameters.Count; protoParamIndex++)
            {
                if (Parameters[protoParamIndex].Type == KbSystemFunctionParameterType.StringInfinite
                    || Parameters[protoParamIndex].Type == KbSystemFunctionParameterType.NumericInfinite)
                {
                    //This is an infinite parameter, and since these are intended to be defined as the last
                    //parameter in the prototype, it eats the remainder of the passed parameters.
                    for (int passedParamIndex = protoParamIndex; passedParamIndex < values.Count; passedParamIndex++)
                    {
                        result.Values.Add(new SystemFunctionParameterValue(Parameters[protoParamIndex], values[passedParamIndex]));
                    }
                    //We return here because we want to skip the parameter count check.
                    //This is ok, because we hit an "infinite parameter".
                    return result;
                }

                if (protoParamIndex >= values.Count)
                {
                    if (Parameters[protoParamIndex].HasDefault)
                    {
                        result.Values.Add(new SystemFunctionParameterValue(Parameters[protoParamIndex], Parameters[protoParamIndex].DefaultValue));
                    }
                    else
                    {
                        throw new KbFunctionException($"Function [{Name}] parameter [{Parameters[protoParamIndex].Name}] passed is not optional.");
                    }
                }
                else
                {
                    result.Values.Add(new SystemFunctionParameterValue(Parameters[protoParamIndex], values[protoParamIndex]));
                }

                satisfiedParameterCount++;
            }

            if (satisfiedParameterCount != Parameters.Count || values.Count > Parameters.Count)
            {
                throw new KbFunctionException($"Incorrect number of parameters passed to [{Name}].");
            }

            return result;
        }
    }
}
