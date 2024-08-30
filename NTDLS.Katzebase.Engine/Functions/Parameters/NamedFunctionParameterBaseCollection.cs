using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Engine.Query;
using NTDLS.Katzebase.Engine.Query.Tokenizers;

namespace NTDLS.Katzebase.Engine.Functions.Parameters
{
    internal class NamedFunctionParameterBaseCollection : KbInsensitiveDictionary<FunctionParameterBase>
    {
        public new void Add(string key, FunctionParameterBase param)
        {
            param.Ordinal = Count;
            base.Add(key, param);
        }

        public void RepopulateStringNumbersAndParameters(QueryTokenizer tokenizer)
        {
            RepopulateStringNumbersAndParameters(this, tokenizer);
        }

        private void RepopulateStringNumbersAndParameters(KbInsensitiveDictionary<FunctionParameterBase> list, QueryTokenizer tokenizer)
        {
            foreach (var param in list)
            {
                if (param.Value is FunctionConstantParameter functionConstantParameter)
                {
                    foreach (var literal in tokenizer.StringLiterals)
                    {
                        string value = literal.Value.Substring(1, literal.Value.Length - 2);
                        functionConstantParameter.RawValue = functionConstantParameter.RawValue.Replace(literal.Key, value);
                    }
                    foreach (var literal in tokenizer.NumericLiterals)
                    {
                        functionConstantParameter.RawValue = functionConstantParameter.RawValue.Replace(literal.Key, literal.Value);
                    }
                    foreach (var literal in tokenizer.UserParameters)
                    {
                        functionConstantParameter.RawValue = functionConstantParameter.RawValue.Replace(literal.Key, InputSanitizer.SanitizeUserInput(literal.Value));
                    }
                }
                else if (param.Value is FunctionExpression functionExpression)
                {
                    foreach (var literal in tokenizer.StringLiterals)
                    {
                        string value = "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"";
                        functionExpression.Value = functionExpression.Value.Replace(literal.Key, value);
                    }
                    foreach (var literal in tokenizer.NumericLiterals)
                    {
                        functionExpression.Value = functionExpression.Value.Replace(literal.Key, literal.Value);
                    }
                    foreach (var literal in tokenizer.UserParameters)
                    {
                        //TODO: Untested
                        functionExpression.Value = functionExpression.Value.Replace(literal.Key, InputSanitizer.SanitizeUserInput(literal.Value));
                    }

                    RepopulateStringNumbersAndParameters(functionExpression.Parameters, tokenizer);
                }
                else if (param.Value is FunctionDocumentFieldParameter functionDocumentFieldParameter)
                {
                    foreach (var literal in tokenizer.StringLiterals)
                    {
                        string value = "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"";
                        functionDocumentFieldParameter.Alias = functionDocumentFieldParameter.Alias.Replace(literal.Key, value);
                        functionDocumentFieldParameter.Value.Field = functionDocumentFieldParameter.Value.Field.Replace(literal.Key, value);
                        functionDocumentFieldParameter.Value.Alias = functionDocumentFieldParameter.Value.Alias.Replace(literal.Key, value);
                    }
                    foreach (var literal in tokenizer.NumericLiterals)
                    {
                        functionDocumentFieldParameter.Alias = functionDocumentFieldParameter.Alias.Replace(literal.Key, literal.Value);
                        functionDocumentFieldParameter.Value.Field = functionDocumentFieldParameter.Value.Field.Replace(literal.Key, literal.Value);
                        functionDocumentFieldParameter.Value.Alias = functionDocumentFieldParameter.Value.Alias.Replace(literal.Key, literal.Value);
                    }
                    foreach (var literal in tokenizer.UserParameters)
                    {
                        //TODO: Untested
                        string value = InputSanitizer.SanitizeUserInput(literal.Value);
                        functionDocumentFieldParameter.Alias = functionDocumentFieldParameter.Alias.Replace(literal.Key, value);
                        functionDocumentFieldParameter.Value.Field = functionDocumentFieldParameter.Value.Field.Replace(literal.Key, value);
                        functionDocumentFieldParameter.Value.Alias = functionDocumentFieldParameter.Value.Alias.Replace(literal.Key, value);
                    }
                }

                if (param.Value is FunctionWithParams functionWithParams)
                {
                    RepopulateStringNumbersAndParameters(functionWithParams.Parameters, tokenizer);
                }
            }
        }

        private void RepopulateStringNumbersAndParameters(List<FunctionParameterBase> list, QueryTokenizer tokenizer)
        {
            foreach (var param in list)
            {
                if (param is FunctionConstantParameter functionConstantParameter)
                {
                    foreach (var literal in tokenizer.StringLiterals)
                    {
                        string value = literal.Value.Substring(1, literal.Value.Length - 2);
                        functionConstantParameter.RawValue = functionConstantParameter.RawValue.Replace(literal.Key, value);
                    }
                    foreach (var literal in tokenizer.NumericLiterals)
                    {
                        functionConstantParameter.RawValue = functionConstantParameter.RawValue.Replace(literal.Key, literal.Value);
                    }
                    foreach (var literal in tokenizer.UserParameters)
                    {
                        //TODO: Untested
                        functionConstantParameter.RawValue = functionConstantParameter.RawValue.Replace(literal.Key, InputSanitizer.SanitizeUserInput(literal.Value));
                    }
                }
                else if (param is FunctionExpression functionExpression)
                {
                    foreach (var literal in tokenizer.StringLiterals)
                    {
                        string value = "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"";
                        functionExpression.Value = functionExpression.Value.Replace(literal.Key, value);
                    }
                    foreach (var literal in tokenizer.NumericLiterals)
                    {
                        functionExpression.Value = functionExpression.Value.Replace(literal.Key, literal.Value);
                    }
                    foreach (var literal in tokenizer.UserParameters)
                    {
                        //TODO: Untested
                        functionExpression.Value = functionExpression.Value.Replace(literal.Key, InputSanitizer.SanitizeUserInput(literal.Value));
                    }

                    RepopulateStringNumbersAndParameters(functionExpression.Parameters, tokenizer);
                }
                else if (param is FunctionDocumentFieldParameter functionDocumentFieldParameter)
                {
                    foreach (var literal in tokenizer.StringLiterals)
                    {
                        string value = "\"" + literal.Value.Substring(1, literal.Value.Length - 2) + "\"";
                        functionDocumentFieldParameter.Alias = functionDocumentFieldParameter.Alias.Replace(literal.Key, value);
                        functionDocumentFieldParameter.Value.Field = functionDocumentFieldParameter.Value.Field.Replace(literal.Key, value);
                        functionDocumentFieldParameter.Value.Alias = functionDocumentFieldParameter.Value.Alias.Replace(literal.Key, value);
                    }
                    foreach (var literal in tokenizer.NumericLiterals)
                    {
                        functionDocumentFieldParameter.Alias = functionDocumentFieldParameter.Alias.Replace(literal.Key, literal.Value);
                        functionDocumentFieldParameter.Value.Field = functionDocumentFieldParameter.Value.Field.Replace(literal.Key, literal.Value);
                        functionDocumentFieldParameter.Value.Alias = functionDocumentFieldParameter.Value.Alias.Replace(literal.Key, literal.Value);
                    }
                    foreach (var literal in tokenizer.UserParameters)
                    {
                        //TODO: Untested
                        string value = InputSanitizer.SanitizeUserInput(literal.Value);
                        functionDocumentFieldParameter.Alias = functionDocumentFieldParameter.Alias.Replace(literal.Key, value);
                        functionDocumentFieldParameter.Value.Field = functionDocumentFieldParameter.Value.Field.Replace(literal.Key, value);
                        functionDocumentFieldParameter.Value.Alias = functionDocumentFieldParameter.Value.Alias.Replace(literal.Key, value);
                    }
                }

                if (param is FunctionWithParams functionWithParams)
                {
                    RepopulateStringNumbersAndParameters(functionWithParams.Parameters, tokenizer);
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