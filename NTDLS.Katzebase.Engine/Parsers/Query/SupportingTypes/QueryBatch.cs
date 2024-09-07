using NTDLS.Katzebase.Client.Types;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
{
    internal class QueryBatch : List<PreparedQuery>
    {
        public KbInsensitiveDictionary<QueryFieldLiteral> Literals { get; set; } = new();

        public QueryBatch(KbInsensitiveDictionary<QueryFieldLiteral> literals)
        {
            Literals = literals;
        }

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
