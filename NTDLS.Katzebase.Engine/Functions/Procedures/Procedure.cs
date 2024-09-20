using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Functions.Parameters;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.Engine.Functions.Procedures
{
    /// <summary>
    /// Contains a parsed procedure prototype.
    /// </summary>
    internal class Procedure
    {
        public string Name { get; private set; }
        public List<ProcedureParameterPrototype> Parameters { get; private set; } = new();

        public Procedure(string name, List<ProcedureParameterPrototype> parameters)
        {
            Name = name;
            Parameters.AddRange(parameters);
        }

        public static Procedure Parse(string prototype)
        {
            int indexOfNameEnd = prototype.IndexOf(':');
            string procedureName = prototype.Substring(0, indexOfNameEnd);
            var parameterStrings = prototype.Substring(indexOfNameEnd + 1).Split(',', StringSplitOptions.RemoveEmptyEntries);
            List<ProcedureParameterPrototype> parameters = new();

            foreach (var param in parameterStrings)
            {
                var typeAndName = param.Split("/");
                if (Enum.TryParse(typeAndName[0], true, out KbProcedureParameterType paramType) == false)
                {
                    throw new KbGenericException($"Unknown parameter type {typeAndName[0]}");
                }

                var nameAndDefault = typeAndName[1].Trim().Split('=');

                if (nameAndDefault.Length == 1)
                {
                    parameters.Add(new ProcedureParameterPrototype(paramType, nameAndDefault[0]));
                }
                else if (nameAndDefault.Length == 2)
                {
                    parameters.Add(new ProcedureParameterPrototype(paramType, nameAndDefault[0],
                        nameAndDefault[1].Is("null") ? null : nameAndDefault[1]));
                }
                else
                {
                    throw new KbGenericException($"Wrong number of default parameters supplied to prototype for {typeAndName[0]}");
                }
            }

            return new Procedure(procedureName, parameters);
        }

        internal ProcedureParameterValueCollection ApplyParameters(List<FunctionParameterBase> values)
        {
            int requiredParameterCount = Parameters.Count(o => o.Type.ToString().ContainsInsensitive("optional") == false);

            if (Parameters.Count < requiredParameterCount)
            {
                if (Parameters.Count > 0 && Parameters[0].Type == KbProcedureParameterType.StringInfinite)
                {
                    //The first parameter is infinite, we don't even check anything else.
                }
                else
                {
                    throw new KbFunctionException($"Incorrect number of parameter passed to {Name}.");
                }
            }

            var result = new ProcedureParameterValueCollection();

            if (Parameters.Count > 0 && Parameters[0].Type == KbProcedureParameterType.StringInfinite)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (values[i] is FunctionExpression functionExpression)
                    {
                        result.Values.Add(new ProcedureParameterValue(Parameters[0], functionExpression.Value));
                    }
                    else
                    {
                        throw new KbNotImplementedException($"Parameter type [{values[i].GetType()}] is not implemented.");
                    }
                }
            }
            else
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (i >= values.Count)
                    {
                        result.Values.Add(new ProcedureParameterValue(Parameters[i]));
                    }
                    else
                    {
                        if (values[i] is FunctionExpression functionExpression)
                        {
                            result.Values.Add(new ProcedureParameterValue(Parameters[i], functionExpression.Value));
                        }
                        else if (values[i] is FunctionConstantParameter functionConstantParameter)
                        {
                            result.Values.Add(new ProcedureParameterValue(Parameters[i], functionConstantParameter.RawValue));
                        }
                        else
                        {
                            throw new KbNotImplementedException($"Parameter type [{values[i].GetType()}] is not implemented.");
                        }
                    }
                }
            }

            return result;
        }
    }
}
