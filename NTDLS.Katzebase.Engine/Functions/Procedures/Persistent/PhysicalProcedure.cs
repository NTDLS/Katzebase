using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Functions.Parameters;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.Engine.Functions.Procedures.Persistent
{
    [Serializable]
    public class PhysicalProcedure
    {
        public List<PhysicalProcedureParameter> Parameters { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public List<string> Batches { get; set; } = new List<string>();

        public PhysicalProcedure Clone()
        {
            return new PhysicalProcedure
            {
                Id = Id,
                Name = Name,
                Created = Created,
                Modified = Modified
            };
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
                    throw new KbFunctionException($"Incorrect number of parameter passed to procedure: [{Name}].");
                }
            }

            var result = new ProcedureParameterValueCollection();

            if (Parameters.Count > 0 && Parameters[0].Type == KbProcedureParameterType.StringInfinite)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (values[i] is FunctionExpression expression)
                    {
                        result.Values.Add(new ProcedureParameterValue(Parameters[0].ToProcedureParameterPrototype(), expression.Value));
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
                        result.Values.Add(new ProcedureParameterValue(Parameters[i].ToProcedureParameterPrototype()));
                    }
                    else
                    {
                        if (values[i] is FunctionExpression functionExpression)
                        {
                            result.Values.Add(new ProcedureParameterValue(Parameters[i].ToProcedureParameterPrototype(), functionExpression.Value));
                        }
                        else if (values[i] is FunctionConstantParameter functionConstantParameter)
                        {
                            result.Values.Add(new ProcedureParameterValue(Parameters[i].ToProcedureParameterPrototype(), functionConstantParameter.RawValue));
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
