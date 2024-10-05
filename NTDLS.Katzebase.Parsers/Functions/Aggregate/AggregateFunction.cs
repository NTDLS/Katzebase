using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Functions.Aggregate
{
    /// <summary>
    /// Contains a parsed function prototype.
    /// </summary>
    public class AggregateFunction
    {
        public string Name { get; set; }
        public string Description { get; private set; }

        public KbAggregateFunctionParameterType ReturnType { get; private set; }

        public List<AggregateFunctionParameterPrototype> Parameters { get; private set; } = new();

        public AggregateFunction(string name, KbAggregateFunctionParameterType returnType, List<AggregateFunctionParameterPrototype> parameters, string description)
        {
            Name = name;
            Description = description;
            ReturnType = returnType;
            Parameters.AddRange(parameters);
        }

        public static AggregateFunction Parse(string prototype)
        {
            var tokenizer = new Tokenizer(prototype, true);

            var returnType = tokenizer.EatIfNextEnum<KbAggregateFunctionParameterType>();

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var functionName) == false)
            {
                throw new KbEngineException($"Invalid aggregate function name: [{functionName}].");
            }

            bool foundOptionalParameter = false;
            bool infiniteParameterFound = false;

            var parameters = new List<AggregateFunctionParameterPrototype>();
            var parametersStrings = tokenizer.EatGetMatchingScope().ScopeSensitiveSplit(',');

            foreach (var parametersString in parametersStrings)
            {
                var paramTokenizer = new Tokenizer(parametersString);

                var paramType = paramTokenizer.EatIfNextEnum<KbAggregateFunctionParameterType>();

                if (paramType == KbAggregateFunctionParameterType.NumericInfinite || paramType == KbAggregateFunctionParameterType.StringInfinite)
                {
                    if (infiniteParameterFound)
                    {
                        throw new KbEngineException($"Invalid aggregate function [{functionName}] prototype. Function cannot contain more than one infinite parameter.");
                    }

                    if (foundOptionalParameter)
                    {
                        throw new KbEngineException($"Invalid aggregate function [{functionName}] prototype. Function cannot contain both infinite parameters and optional parameters.");
                    }

                    infiniteParameterFound = true;
                }

                if (paramTokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var parameterName) == false)
                {
                    throw new KbEngineException($"Invalid aggregate function [{functionName}] parameter name: [{parameterName}].");
                }

                if (!paramTokenizer.IsExhausted()) //Parse optional parameter default value.
                {
                    if (infiniteParameterFound)
                    {
                        throw new KbEngineException($"Invalid aggregate function [{functionName}] prototype. Function cannot contain both infinite parameters and optional parameters.");
                    }

                    if (paramTokenizer.TryEatIfNextCharacter('=') == false)
                    {
                        throw new KbEngineException($"Invalid aggregate function [{functionName}] prototype when parsing optional parameter [{parameterName}]. Expected [=], found: [{paramTokenizer.NextCharacter}].");
                    }

                    var optionalParameterDefaultValue = tokenizer.ResolveLiteral(paramTokenizer.EatGetNext());
                    if (optionalParameterDefaultValue == null || optionalParameterDefaultValue?.Is("null") == true)
                    {
                        optionalParameterDefaultValue = null;
                    }

                    parameters.Add(new AggregateFunctionParameterPrototype(paramType, parameterName, optionalParameterDefaultValue));

                    foundOptionalParameter = true;
                }
                else
                {
                    if (foundOptionalParameter)
                    {
                        //If we have already found an optional parameter, then all remaining parameters must be optional
                        throw new KbEngineException($"Invalid aggregate function [{functionName}] parameter [{parameterName}] must define a default.");
                    }

                    parameters.Add(new AggregateFunctionParameterPrototype(paramType, parameterName));
                }

                if (paramType == KbAggregateFunctionParameterType.StringInfinite)
                {
                    if (!tokenizer.IsExhausted())
                    {
                        throw new KbEngineException($"Failed to parse aggregate function [{functionName}] prototype, infinite parameter [{parameterName}] must be the last parameter.");
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
                throw new KbEngineException($"Failed to parse aggregate function [{functionName}] prototype, expected end-of-line: [{tokenizer.Remainder}].");
            }

            return new AggregateFunction(functionName, returnType, parameters, description);
        }

        internal AggregateFunctionParameterValueCollection ApplyParameters(List<string?> values)
        {
            var result = new AggregateFunctionParameterValueCollection();

            int satisfiedParameterCount = 1; //Compensate for the aggregate value list which is not really passed by parameter.

            for (int protoParamIndex = 1; protoParamIndex < Parameters.Count; protoParamIndex++) //Compensate for the aggregate value list which is not really passed by parameter.
            {
                if (Parameters[protoParamIndex].Type == KbAggregateFunctionParameterType.StringInfinite)
                {
                    //This is an infinite parameter, and since these are intended to be defined as the last
                    //parameter in the prototype, it eats the remainder of the passed parameters.
                    for (int passedParamIndex = protoParamIndex; passedParamIndex < values.Count; passedParamIndex++)
                    {
                        result.Values.Add(new AggregateFunctionParameterValue(Parameters[protoParamIndex], values[passedParamIndex]));
                    }
                    break;
                }

                if (protoParamIndex > values.Count)
                {
                    if (Parameters[protoParamIndex].HasDefault)
                    {
                        result.Values.Add(new AggregateFunctionParameterValue(Parameters[protoParamIndex], Parameters[protoParamIndex].DefaultValue));
                    }
                    else
                    {
                        throw new KbFunctionException($"Function [{Name}] parameter [{Parameters[protoParamIndex].Name}] passed is not optional.");
                    }
                }
                else
                {
                    result.Values.Add(new AggregateFunctionParameterValue(Parameters[protoParamIndex], values[protoParamIndex - 1]));
                }

                satisfiedParameterCount++;
            }

            if (satisfiedParameterCount != Parameters.Count)
            {
                throw new KbFunctionException($"Incorrect number of parameters passed to aggregate function: [{Name}].");
            }

            return result;
        }
    }
}
