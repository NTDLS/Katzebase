using NTDLS.Katzebase.Client.Types;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
{
    internal class QueryBatch : List<PreparedQuery>
    {
        public KbInsensitiveDictionary<string> UserParameters { get; set; } = new();

        public KbInsensitiveDictionary<QueryFieldLiteral> Literals { get; set; } = new();

        public QueryBatch(KbInsensitiveDictionary<string> userParameters, KbInsensitiveDictionary<QueryFieldLiteral> literals)
        {
            UserParameters = userParameters;
            Literals = literals;
        }

        public string GetLiteralValue(string value)
        {
            if (Literals.TryGetValue(value, out var literal))
            {
                return literal.Value;
            }
            else return value;
        }

        public string GetLiteralValue(string value, out BasicDataType outDataType)
        {
            if (Literals.TryGetValue(value, out var literal))
            {
                outDataType = BasicDataType.String;
                return literal.Value;
            }

            outDataType = BasicDataType.Undefined;
            return value;
        }
    }
}
