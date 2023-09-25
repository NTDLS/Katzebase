using Katzebase.Engine.Functions.Parameters;
using Katzebase.Exceptions;

namespace Katzebase.Engine.Functions.Procedures
{
    /// <summary>
    /// Contains a parsed procedure prototype.
    /// </summary>
    internal class Procedure
    {
        public string Name { get; set; }
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

                if (nameAndDefault.Count() == 1)
                {
                    parameters.Add(new ProcedureParameterPrototype(paramType, nameAndDefault[0]));
                }
                else if (nameAndDefault.Count() == 2)
                {
                    parameters.Add(new ProcedureParameterPrototype(paramType, nameAndDefault[0],
                        nameAndDefault[1].ToLower() == "null" ? null : nameAndDefault[1]));
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
            int requiredParameterCount = Parameters.Where(o => o.Type.ToString().ToLower().Contains("optional") == false).Count();

            if (Parameters.Count < requiredParameterCount)
            {
                if (Parameters.Count > 0 && Parameters[0].Type == KbProcedureParameterType.Infinite_String)
                {
                    //The first parameter is infinite, we dont even check anything else.
                }
                else
                {
                    throw new KbFunctionException($"Incorrect number of parameter passed to {Name}.");
                }
            }

            var result = new ProcedureParameterValueCollection();

            if (Parameters.Count > 0 && Parameters[0].Type == KbProcedureParameterType.Infinite_String)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (values[i] is FunctionExpression)
                    {
                        var expression = (FunctionExpression)values[i];
                        result.Values.Add(new ProcedureParameterValue(Parameters[0], expression.Value));
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
                        if (values[i] is FunctionExpression)
                        {
                            var expression = (FunctionExpression)values[i];
                            result.Values.Add(new ProcedureParameterValue(Parameters[i], expression.Value));
                        }
                        else if (values[i] is FunctionConstantParameter)
                        {
                            var constant = (FunctionConstantParameter)values[i];
                            result.Values.Add(new ProcedureParameterValue(Parameters[i], constant.RawValue));
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
