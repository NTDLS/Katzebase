using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserCreate
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token = tokenizer.EatGetNext();

            if (StaticParserUtility.IsStartOfQuery(token, out var queryType) == false)
            {
                string acceptableValues = string.Join("', '", Enum.GetValues<QueryType>().Where(o => o != QueryType.None));
                throw new KbParserException($"Invalid query. Found '{token}', expected: '{acceptableValues}'.");
            }

            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Schema, SubQueryType.Index, SubQueryType.UniqueKey, SubQueryType.Procedure]);

            return querySubType switch
            {
                SubQueryType.Schema => StaticParserCreateSchema.Parse(queryBatch, tokenizer),
                SubQueryType.Index => StaticParserCreateIndex.Parse(queryBatch, tokenizer),
                SubQueryType.UniqueKey => StaticParserCreateUniqueKey.Parse(queryBatch, tokenizer),
                SubQueryType.Procedure => StaticParserCreateProcedure.Parse(queryBatch, tokenizer),

                _ => throw new KbParserException($"The query type is not implemented: [{token}]."),
            };
        }
    }
}
