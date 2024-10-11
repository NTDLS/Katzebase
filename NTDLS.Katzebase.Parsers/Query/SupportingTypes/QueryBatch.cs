using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Api.KbConstants;


namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class QueryBatch(QueryVariables variables)
        : List<PreparedQuery>
    {
        public QueryVariables Variables { get; set; } = variables;

        public string? GetLiteralValue(string? value)
        {
            if (value == null) return null;

            if (Variables.Collection.TryGetValue(value, out var literal))
            {
                if (literal.DataType == KbBasicDataType.Undefined)
                {
                    if (Variables.VariableReverseLookup.TryGetValue(value, out var variableName))
                    {
                        throw new KbParserException($"Variable is undefined: [{variableName}].");
                    }

                    throw new KbParserException("Variable is undefined.");
                }

                return literal.Value?.AssertUnresolvedExpression();
            }
            else return value?.AssertUnresolvedExpression();
        }

        public string? GetLiteralValue(string? value, out KbBasicDataType outDataType)
        {
            if (value != null)
            {
                if (Variables.Collection.TryGetValue(value, out var literal))
                {
                    if (literal.DataType == KbBasicDataType.Undefined)
                    {
                        if (Variables.VariableReverseLookup.TryGetValue(value, out var variableName))
                        {
                            throw new KbParserException($"Variable is undefined: [{variableName}].");
                        }

                        throw new KbParserException("Variable is undefined.");
                    }

                    outDataType = literal.DataType;
                    return literal.Value?.AssertUnresolvedExpression();
                }
            }
            outDataType = KbBasicDataType.Undefined;
            return value?.AssertUnresolvedExpression();
        }
    }
}
