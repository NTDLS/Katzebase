using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Api.KbConstants;


namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class QueryBatch(KbInsensitiveDictionary<KbVariable> literals)
        : List<PreparedQuery>
    {
        public KbInsensitiveDictionary<KbVariable> Variables { get; set; } = literals;

        public string? GetLiteralValue(string? value)
        {
            if (value == null) return null;

            if (Variables.TryGetValue(value, out var literal))
            {
                if (literal.DataType == KbBasicDataType.Undefined)
                {
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
                if (Variables.TryGetValue(value, out var literal))
                {
                    if (literal.DataType == KbBasicDataType.Undefined)
                    {
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
