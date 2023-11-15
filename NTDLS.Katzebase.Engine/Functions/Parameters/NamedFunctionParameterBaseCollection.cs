using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Query;

namespace NTDLS.Katzebase.Engine.Functions.Parameters
{
    internal class NamedFunctionParameterBaseCollection : KbInsensitiveDictionary<FunctionParameterBase>
    {
        public new void Add(string key, FunctionParameterBase param)
        {
            param.Ordinal = Count;
            base.Add(key, param);
        }

        public void RefillStringLiterals(KbInsensitiveDictionary<string> literals)
        {
            RefillStringLiterals(this, literals);
        }

        private void RefillStringLiterals(KbInsensitiveDictionary<FunctionParameterBase> list, KbInsensitiveDictionary<string> literals)
        {
            foreach (var param in list)
            {
                if (param.Value is FunctionConstantParameter)
                {
                    foreach (var literal in literals)
                    {
                        ((FunctionConstantParameter)param.Value).RawValue = ((FunctionConstantParameter)param.Value).RawValue.Replace(literal.Key, literal.Value.Substring(1, literal.Value.Length - 2));
                    }
                }
                else if (param.Value is FunctionExpression)
                {
                    foreach (var literal in literals)
                    {
                        ((FunctionExpression)param.Value).Value = ((FunctionExpression)param.Value).Value.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                    }
                }
                else if (param.Value is FunctionDocumentFieldParameter)
                {
                    foreach (var literal in literals)
                    {
                        ((FunctionDocumentFieldParameter)param.Value).Alias = ((FunctionDocumentFieldParameter)param.Value).Alias.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                        ((FunctionDocumentFieldParameter)param.Value).Value.Field = ((FunctionDocumentFieldParameter)param.Value).Value.Field.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                        ((FunctionDocumentFieldParameter)param.Value).Value.Alias = ((FunctionDocumentFieldParameter)param.Value).Value.Alias.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                    }
                }

                if (param.Value is FunctionExpression)
                {
                    RefillStringLiterals(((FunctionExpression)param.Value).Parameters, literals);
                }
                if (param.Value is FunctionWithParams)
                {
                    RefillStringLiterals(((FunctionWithParams)param.Value).Parameters, literals);
                }
            }
        }

        private void RefillStringLiterals(List<FunctionParameterBase> list, KbInsensitiveDictionary<string> literals)
        {
            foreach (var param in list)
            {
                if (param is FunctionConstantParameter)
                {
                    foreach (var literal in literals)
                    {
                        ((FunctionConstantParameter)param).RawValue = ((FunctionConstantParameter)param).RawValue.Replace(literal.Key, literal.Value.Substring(1, literal.Value.Length - 2));
                    }
                }
                else if (param is FunctionExpression)
                {
                    foreach (var literal in literals)
                    {
                        ((FunctionExpression)param).Value = ((FunctionExpression)param).Value.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                    }
                }
                else if (param is FunctionDocumentFieldParameter)
                {
                    foreach (var literal in literals)
                    {
                        ((FunctionDocumentFieldParameter)param).Alias = ((FunctionDocumentFieldParameter)param).Alias.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                        ((FunctionDocumentFieldParameter)param).Value.Field = ((FunctionDocumentFieldParameter)param).Value.Field.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                        ((FunctionDocumentFieldParameter)param).Value.Alias = ((FunctionDocumentFieldParameter)param).Value.Alias.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                    }
                }

                if (param is FunctionExpression)
                {
                    RefillStringLiterals(((FunctionExpression)param).Parameters, literals);
                }
                if (param is FunctionWithParams)
                {
                    RefillStringLiterals(((FunctionWithParams)param).Parameters, literals);
                }
            }
        }


        private void GetAllFieldsRecursive(ref List<PrefixedField> result, List<FunctionParameterBase> list)
        {
            foreach (var param in list)
            {
                if (param is FunctionDocumentFieldParameter)
                {
                    var key = ((FunctionDocumentFieldParameter)param).Value.Key;

                    if (result.Any(o => o.Key == key) == false)
                    {
                        result.Add(((FunctionDocumentFieldParameter)param).Value);
                    }
                }

                if (param is FunctionExpression)
                {
                    GetAllFieldsRecursive(ref result, ((FunctionExpression)param).Parameters);
                }
                if (param is FunctionWithParams)
                {
                    GetAllFieldsRecursive(ref result, ((FunctionWithParams)param).Parameters);
                }
            }
        }
    }
}