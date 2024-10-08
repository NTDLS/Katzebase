using NTDLS.Katzebase.Api.Types;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class QueryBatch(KbInsensitiveDictionary<ConditionFieldLiteral> literals)
        : List<PreparedQuery>
    {
        public KbInsensitiveDictionary<ConditionFieldLiteral> Literals { get; set; } = literals;

        public string? GetLiteralValue(string value)
        {
            if (Literals.TryGetValue(value, out var literal))
            {
                return literal.Value;
            }
            else return value;
        }

        public string? GetLiteralValue(string value, out KbBasicDataType outDataType)
        {
            if (Literals.TryGetValue(value, out var literal))
            {
                outDataType = KbBasicDataType.String;
                return literal.Value;
            }

            outDataType = KbBasicDataType.Undefined;
            return value;
        }
    }
}
