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
                if (param.Value is FunctionConstantParameter functionConstantParameter)
                {
                    foreach (var literal in literals)
                    {
                        functionConstantParameter.RawValue = functionConstantParameter.RawValue.Replace(literal.Key, literal.Value.Substring(1, literal.Value.Length - 2));
                    }
                }
                else if (param.Value is FunctionExpression functionExpression)
                {
                    foreach (var literal in literals)
                    {
                        functionExpression.Value = functionExpression.Value.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                    }

                    RefillStringLiterals(functionExpression.Parameters, literals);
                }
                else if (param.Value is FunctionDocumentFieldParameter functionDocumentFieldParameter)
                {
                    foreach (var literal in literals)
                    {
                        functionDocumentFieldParameter.Alias = functionDocumentFieldParameter.Alias.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                        functionDocumentFieldParameter.Value.Field = functionDocumentFieldParameter.Value.Field.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                        functionDocumentFieldParameter.Value.Alias = functionDocumentFieldParameter.Value.Alias.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                    }
                }

                if (param.Value is FunctionWithParams functionWithParams)
                {
                    RefillStringLiterals(functionWithParams.Parameters, literals);
                }
            }
        }

        private void RefillStringLiterals(List<FunctionParameterBase> list, KbInsensitiveDictionary<string> literals)
        {
            foreach (var param in list)
            {
                if (param is FunctionConstantParameter functionConstantParameter)
                {
                    foreach (var literal in literals)
                    {
                        functionConstantParameter.RawValue = functionConstantParameter.RawValue.Replace(literal.Key, literal.Value.Substring(1, literal.Value.Length - 2));
                    }
                }
                else if (param is FunctionExpression functionExpression)
                {
                    foreach (var literal in literals)
                    {
                        functionExpression.Value = functionExpression.Value.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                    }

                    RefillStringLiterals(functionExpression.Parameters, literals);
                }
                else if (param is FunctionDocumentFieldParameter functionDocumentFieldParameter)
                {
                    foreach (var literal in literals)
                    {
                        functionDocumentFieldParameter.Alias = functionDocumentFieldParameter.Alias.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                        functionDocumentFieldParameter.Value.Field = functionDocumentFieldParameter.Value.Field.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                        functionDocumentFieldParameter.Value.Alias = functionDocumentFieldParameter.Value.Alias.Replace(literal.Key, "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"");
                    }
                }

                if (param is FunctionWithParams functionWithParams)
                {
                    RefillStringLiterals(functionWithParams.Parameters, literals);
                }
            }
        }


        private void GetAllFieldsRecursive(ref List<PrefixedField> result, List<FunctionParameterBase> list)
        {
            foreach (var param in list)
            {
                if (param is FunctionDocumentFieldParameter functionDocumentFieldParameter)
                {
                    var key = functionDocumentFieldParameter.Value.Key;

                    if (result.Any(o => o.Key == key) == false)
                    {
                        result.Add(functionDocumentFieldParameter.Value);
                    }
                }

                if (param is FunctionExpression functionExpression)
                {
                    GetAllFieldsRecursive(ref result, functionExpression.Parameters);
                }
                else if (param is FunctionWithParams functionWithParams)
                {
                    GetAllFieldsRecursive(ref result, functionWithParams.Parameters);
                }
            }
        }
    }
}