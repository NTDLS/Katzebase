using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserSample
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Sample)
            {
                //SubQueryType = tokenizer.EatIfNextEnum([SubQueryType.None])
            };

            throw new NotImplementedException("reimplement");

            /*
                result.SubQueryType = SubQueryType.Documents;

                token = tokenizer.GetNext();
                if (token == string.Empty)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: schema name.");
                }

                result.Schemas.Add(new QuerySchema(token));

                token = tokenizer.GetNext();
                if (token != string.Empty)
                {
                    if (int.TryParse(token, out int topCount) == false)
                    {
                        throw new KbParserException("Invalid query. Found '" + token + "', expected: numeric top count.");
                    }
                    result.RowLimit = topCount;
                }
                else
                {
                    result.RowLimit = 100;
                }
            */
        }
    }
}
