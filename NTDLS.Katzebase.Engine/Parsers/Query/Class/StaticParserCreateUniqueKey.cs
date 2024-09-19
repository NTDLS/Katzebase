using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserCreateUniqueKey
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token;

            var query = new PreparedQuery(queryBatch, QueryType.Create)
            {
                SubQueryType = SubQueryType.UniqueKey
            };

            throw new NotImplementedException("Reimplement this query type.");

            /*
                query.AddAttribute(PreparedQuery.QueryAttribute.IndexName, token);
                query.AddAttribute(PreparedQuery.QueryAttribute.IsUnique, (subQueryType == SubQueryType.UniqueKey));


                if (tokenizer.NextCharacter != '(')
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.NextCharacter + "', expected: ','.");
                }
                tokenizer.SkipDelimiters('(');

                while (true) //Get fields
                {
                    token = tokenizer.GetNext().ToLowerInvariant();
                    if (token == string.Empty)
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.PeekNext() + "', expected: ',' or ')'.");
                    }

                    query.CreateFields.Add(token);

                    if (tokenizer.NextCharacter == ',')
                    {
                        tokenizer.SkipDelimiters(',');
                    }
                    if (tokenizer.NextCharacter == ')')
                    {
                        tokenizer.SkipDelimiters(')');
                        break;
                    }
                }

                if (tokenizer.GetNext().Is("on") == false)
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: 'on'.");
                }

                token = tokenizer.GetNext();
                if (!TokenHelpers.IsValidIdentifier(token, ':'))
                {
                    throw new KbParserException("Invalid query. Found '" + token + "', expected: schema name.");
                }

                query.Schemas.Add(new QuerySchema(token));

                if (tokenizer.TryIsNextToken("with"))
                {
                    var options = new ExpectedWithOptions
                        {
                            {"partitions", typeof(uint) }
                        };
                    StaticWithOptionsParser.ParseWithOptions(ref tokenizer, options, ref query);
                }
            */

            return query;
        }
    }
}
