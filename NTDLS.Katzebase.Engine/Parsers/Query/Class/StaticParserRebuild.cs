using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserRebuild
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Rebuild)
            {
                //SubQueryType = SubQueryType.None
            };

            throw new NotImplementedException("reimplement");

            /*
                if (tokenizer.PeekNext().IsOneOf(["index", "uniquekey"]) == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: 'index' or 'uniquekey'.");
                }

                token = tokenizer.GetNext();
                if (Enum.TryParse<SubQueryType>(token, true, out SubQueryType subQueryType) == false)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: 'index' or 'uniquekey'.");
                }
                result.SubQueryType = subQueryType;

                token = tokenizer.GetNext();
                if (token == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: index name.");
                }
                result.AddAttribute(PreparedQuery.QueryAttribute.IndexName, token);

                if (tokenizer.GetNext().Is("on") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: 'on'.");
                }

                token = tokenizer.GetNext();
                if (token == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
                }

                result.Schemas.Add(new QuerySchema(token));

                if (tokenizer.PeekNext().Is("with"))
                {
                    var options = new ExpectedWithOptions
                    {
                        {"partitions", typeof(uint) }
                    };
                    StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
                }
            */
        }
    }
}
