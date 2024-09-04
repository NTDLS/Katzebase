using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Query.Tokenizers;

namespace NTDLS.Katzebase.Engine.Functions.Parameters
{
    internal class NamedFunctionParameterBaseCollection : KbInsensitiveDictionary<FunctionParameterBase>
    {
        private void RepopulateLiterals(List<FunctionParameterBase> list, QueryTokenizer tokenizer)
        {
            foreach (var param in list)
            {
                if (param is FunctionConstantParameter functionConstantParameter)
                {
                    functionConstantParameter.RawValue = tokenizer.GetLiteralValue(functionConstantParameter.FinalValue);
                }
                else if (param is FunctionExpression functionExpression)
                {
                    functionExpression.Value = tokenizer.GetLiteralValue(functionExpression.Value);
                    RepopulateLiterals(functionExpression.Parameters, tokenizer);
                }
                else if (param is FunctionDocumentFieldParameter functionDocumentFieldParameter)
                {
                    functionDocumentFieldParameter.Alias = tokenizer.GetLiteralValue(functionDocumentFieldParameter.Alias);
                    functionDocumentFieldParameter.Value.Field = tokenizer.GetLiteralValue(functionDocumentFieldParameter.Value.Field);
                    functionDocumentFieldParameter.Value.Alias = tokenizer.GetLiteralValue(functionDocumentFieldParameter.Value.Alias);
                }

                if (param is FunctionWithParams functionWithParams)
                {
                    RepopulateLiterals(functionWithParams.Parameters, tokenizer);
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