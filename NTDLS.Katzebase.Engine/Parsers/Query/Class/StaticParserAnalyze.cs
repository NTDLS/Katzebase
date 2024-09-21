using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserAnalyze
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Analyze)
            {
                //SubQueryType = SubQueryType.None
            };

            throw new NotImplementedException("reimplement");

            /*
                            if (tokenizer.PeekNext().IsOneOf(["index", "schema"]) == false)
                            {
                                throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: 'index' or 'schema'.");
                            }

                            token = tokenizer.GetNext();
                            if (Enum.TryParse<SubQueryType>(token, true, out SubQueryType subQueryType) == false)
                            {
                                throw new KbParserException("Invalid query. Found '" + token + "', expected: 'index' or 'schema'.");
                            }

                            result.SubQueryType = subQueryType;

                            if (result.SubQueryType == SubQueryType.Index)
                            {

                                token = tokenizer.GetNext();
                                if (token == string.Empty)
                                {
                                    throw new KbParserException("Invalid query. Found '" + token + "', expected: object name.");
                                }
                                result.AddAttribute(PreparedQuery.QueryAttribute.IndexName, token);

                                if (tokenizer.GetNext().Is("on") == false)
                                {
                                    throw new KbParserException("Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: 'on'.");
                                }

                                token = tokenizer.GetNext();
                                if (token == string.Empty)
                                {
                                    throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
                                }
                                result.AddAttribute(PreparedQuery.QueryAttribute.Schema, token);
                            }
                            else if (result.SubQueryType == SubQueryType.Schema)
                            {
                                token = tokenizer.GetNext();
                                if (token == string.Empty)
                                {
                                    throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
                                }
                                result.AddAttribute(PreparedQuery.QueryAttribute.Schema, token);
                                result.Schemas.Add(new QuerySchema(token));

                                if (tokenizer.PeekNext().Is("with"))
                                {
                                    var options = new ExpectedWithOptions
                                    {
                                        {"includephysicalpages", typeof(bool) }
                                    };
                                    StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref result);
                                }
                            }
                            else
                            {
                                throw new KbParserException("Invalid query. Found '" + token + "', expected: 'index' or 'schema'.");
                            }
            */

            return query;
        }
    }
}
