using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserAlterSchema
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Alter)
            {
                SubQueryType = tokenizer.EatIfNextEnum([SubQueryType.Schema])
            };

            /*
            result.AddAttribute(PreparedQuery.QueryAttribute.IsUnique, (subQueryType == SubQueryType.UniqueKey));

            token = tokenizer.GetNext();
            if (token == string.Empty)
            {
                throw new KbParserException("Invalid query. Found '" + token + "', expected: object name.");
            }
            result.Schemas.Add(new QuerySchema(token));

            if (tokenizer.PeekNext().Is("with"))
            {
                var options = new ExpectedWithOptions
                                    {
                                        {"pagesize", typeof(uint) }
                                    };
                StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
            }
        */

            return query;
        }
    }
}
