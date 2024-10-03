using NTDLS.Katzebase.Client.Types;
using static NTDLS.Katzebase.Client.KbConstants;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class QueryBatch<TData> : List<PreparedQuery<TData>> where TData : IStringable
    {
        public KbInsensitiveDictionary<ConditionFieldLiteral<TData>> Literals { get; set; } = new();

        public QueryBatch(KbInsensitiveDictionary<ConditionFieldLiteral<TData>> literals, Func<string, TData> parse)
        {
            Literals = literals;
            Parse = parse;
        }

        public Func<string, TData> Parse;

        public TData? GetLiteralValue(string value)
        {
            if (Literals.TryGetValue(value, out var literal))
            {
                return literal.Value;
            }
            else return Parse(value); //.ParseToT<TData>(EngineCore<TData>.StrCast);
        }

        public TData? GetLiteralValue(string value, out KbBasicDataType outDataType)
        {
            if (Literals.TryGetValue(value, out var literal))
            {
                outDataType = KbBasicDataType.String;
                return literal.Value;
            }

            outDataType = KbBasicDataType.Undefined;
            return Parse(value); //.ParseToT<TData>(EngineCore<TData>.StrCast);
        }
    }
}
