using Katzebase.Engine.Query.FunctionParameter;
using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Query.Function.Procedures
{
    /// <summary>
    /// Contains a parsed function prototype.
    /// </summary>
    internal class QueryProcedure
    {
        public string Name { get; set; }
        public List<QueryProcedureParameterPrototype> Parameters { get; private set; } = new();

        public QueryProcedure(string name, List<QueryProcedureParameterPrototype> parameters)
        {
            Name = name;
            Parameters.AddRange(parameters);
        }

        public static QueryProcedure Parse(string prototype)
        {
            int indexOfNameEnd = prototype.IndexOf(':');
            string functionName = prototype.Substring(0, indexOfNameEnd);
            var parameterStrings = prototype.Substring(indexOfNameEnd + 1).Split(',', StringSplitOptions.RemoveEmptyEntries);
            List<QueryProcedureParameterPrototype> parameters = new();

            foreach (var param in parameterStrings)
            {
                var typeAndName = param.Split("/");
                if (Enum.TryParse(typeAndName[0], true, out KbQueryProcedureParameterType paramType) == false)
                {
                    throw new KbGenericException($"Unknown parameter type {typeAndName[0]}");
                }

                var nameAndDefault = typeAndName[1].Trim().Split('=');

                if (nameAndDefault.Count() == 1)
                {
                    parameters.Add(new QueryProcedureParameterPrototype(paramType, nameAndDefault[0]));
                }
                else if (nameAndDefault.Count() == 2)
                {
                    parameters.Add(new QueryProcedureParameterPrototype(paramType, nameAndDefault[0],
                        nameAndDefault[1].ToLower() == "null" ? null : nameAndDefault[1]));
                }
                else
                {
                    throw new KbGenericException($"Wrong number of default parameters supplied to prototype for {typeAndName[0]}");
                }
            }

            return new QueryProcedure(functionName, parameters);
        }

        internal QueryProcedureParameterValueCollection ApplyParameters(List<FunctionParameterBase> values)
        {
            int requiredParameterCount = Parameters.Where(o => o.Type.ToString().ToLower().Contains("optional") == false).Count();

            if (Parameters.Count < requiredParameterCount)
            {
                if (Parameters.Count > 0 && Parameters[0].Type == KbQueryProcedureParameterType.Infinite_String)
                {
                    //The first parameter is infinite, we dont even check anything else.
                }
                else
                {
                    throw new KbFunctionException($"Incorrect number of parameter passed to {Name}.");
                }
            }

            var result = new QueryProcedureParameterValueCollection();

            if (Parameters.Count > 0 && Parameters[0].Type == KbQueryProcedureParameterType.Infinite_String)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (values[i] is FunctionExpression)
                    {
                        var expression = (FunctionExpression)values[i];
                        result.Values.Add(new QueryProcedureParameterValue(Parameters[0], expression.Value));
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
                        result.Values.Add(new QueryProcedureParameterValue(Parameters[i]));
                    }
                    else
                    {
                        if (values[i] is FunctionExpression)
                        {
                            var expression = (FunctionExpression)values[i];
                            result.Values.Add(new QueryProcedureParameterValue(Parameters[i], expression.Value));
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
