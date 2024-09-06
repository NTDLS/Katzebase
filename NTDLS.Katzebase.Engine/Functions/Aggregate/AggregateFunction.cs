using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    /// <summary>
    /// Contains a parsed function prototype.
    /// </summary>
    public class AggregateFunction
    {
        public string Name { get; set; }
        public List<AggregateFunctionParameterPrototype> Parameters { get; private set; } = new();

        public AggregateFunction(string name, List<AggregateFunctionParameterPrototype> parameters)
        {
            Name = name;
            Parameters.AddRange(parameters);
        }

        public static AggregateFunction Parse(string prototype)
        {
            int indexOfNameEnd = prototype.IndexOf(':');
            string functionName = prototype.Substring(0, indexOfNameEnd);
            var parameterStrings = prototype.Substring(indexOfNameEnd + 1).Split(',', StringSplitOptions.RemoveEmptyEntries);
            var parameters = new List<AggregateFunctionParameterPrototype>();

            foreach (var param in parameterStrings)
            {
                var typeAndName = param.Split("/");
                if (Enum.TryParse(typeAndName[0], true, out KbAggregateFunctionParameterType paramType) == false)
                {
                    throw new KbGenericException($"Unknown parameter type {typeAndName[0]}");
                }

                var nameAndDefault = typeAndName[1].Trim().Split('=');

                if (nameAndDefault.Length == 1)
                {
                    parameters.Add(new AggregateFunctionParameterPrototype(paramType, nameAndDefault[0]));
                }
                else if (nameAndDefault.Length == 2)
                {
                    parameters.Add(new AggregateFunctionParameterPrototype(paramType, nameAndDefault[0],
                        nameAndDefault[1].Is("null") ? null : nameAndDefault[1]));
                }
                else
                {
                    throw new KbGenericException($"Wrong number of default parameters supplied to prototype for {typeAndName[0]}");
                }
            }

            return new AggregateFunction(functionName, parameters);
        }
    }
}
