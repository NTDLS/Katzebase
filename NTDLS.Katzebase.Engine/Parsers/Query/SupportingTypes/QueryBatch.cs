using NTDLS.Katzebase.Client.Types;
using static NTDLS.Katzebase.Client.KbConstants;
using fs;
namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
{
    internal class QueryBatch : List<PreparedQuery>
    {
        public KbInsensitiveDictionary<ConditionFieldLiteral> Literals { get; set; } = new();

        public QueryBatch(KbInsensitiveDictionary<ConditionFieldLiteral> literals)
        {
            Literals = literals;
        }

        public fstring? GetLiteralValue(string value)
        {
            if (Literals.TryGetValue(value, out var literal))
            {
                return literal.Value;
            }
            else return fstring.NewS(value);
        }

        public fstring? GetLiteralValue(fstring value, out KbBasicDataType outDataType)
        {
            if (Literals.TryGetValue(value.s, out var literal))
            {
                outDataType = KbBasicDataType.String;
                return literal.Value;
            }

            outDataType = KbBasicDataType.Undefined;
            return value;
        }
    }
}
