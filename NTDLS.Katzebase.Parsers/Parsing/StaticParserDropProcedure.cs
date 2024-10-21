using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Parsing
{
    public static class StaticParserDropProcedure
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Drop, tokenizer.GetCurrentLineNumber())
            {
                SubQueryType = SubQueryType.Procedure
            };

            throw new NotImplementedException("Reimplement this query type.");

            /*
                query.AddAttribute(Query.QueryAttribute.ObjectName, token);

                var parameters = new List<PhysicalProcedureParameter>();

                if (tokenizer.NextCharacter == '(') //Parse parameters
                {
                    tokenizer.SkipNextCharacter();

                    while (true)
                    {
                        var paramName = tokenizer.GetNext();
                        if (tokenizer.GetNext().Is("as") == false)
                        {
                            throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: 'AS'.");
                        }
                        token = tokenizer.GetNext();

                        if (Enum.TryParse(token, true, out KbProcedureParameterType paramType) == false || Enum.IsDefined(typeof(KbProcedureParameterType), paramType) == false)
                        {
                            string acceptableValues = string.Join("', '",
                                Enum.GetValues<KbProcedureParameterType>().Where(o => o != KbProcedureParameterType.Undefined));

                            throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found '{token}', expected: '{acceptableValues}'.");
                        }

                        parameters.Add(new PhysicalProcedureParameter(paramName, paramType));

                        if (tokenizer.NextCharacter != ',')
                        {
                            if (tokenizer.NextCharacter != ')')
                            {
                                throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Invalid query. Found '" + tokenizer.NextCharacter + "', expected: ')'.");
                            }
                            tokenizer.SkipNextCharacter();
                            break;
                        }
                        tokenizer.SkipNextCharacter();
                    }
                }

                query.AddAttribute(Query.QueryAttribute.Parameters, parameters);

                if (tokenizer.GetNext().Is("on") == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: 'ON'.");
                }

                token = tokenizer.GetNext();
                if (token == string.Empty)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Invalid query. Found '" + token + "', expected: schema name.");
                }

                query.AddAttribute(Query.QueryAttribute.Schema, token);

                if (tokenizer.GetNext().Is("as") == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Invalid query. Found '" + tokenizer.Breadcrumbs.Last() + "', expected: 'AS'.");
                }

                if (tokenizer.NextCharacter != '(')
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Invalid query. Found '" + tokenizer.NextCharacter + "', expected: '('.");
                }

                if (tokenizer.Remainder().Last() != ')')
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Invalid query. Found '" + tokenizer.NextCharacter + "', expected: ')'.");
                }

                tokenizer.SkipNextCharacter(); // Skip the '('.

                var batches = new List<string>();

                int previousPosition = tokenizer.Caret;

                while (tokenizer.IsEnd() == false)
                {
                    if (tokenizer.NextCharacter == ')')
                    {
                        tokenizer.SkipNextCharacter();
                    }
                    else
                    {
                        _ = PrepareNextQuery(tokenizer);

                        string queryText = tokenizer.Text.Substring(previousPosition, tokenizer.Caret - previousPosition).Trim();

                        foreach (var literalString in tokenizer.StringLiterals)
                        {
                            queryText = queryText.Replace(literalString.Key, literalString.Value);
                        }

                        batches.Add(queryText);

                        previousPosition = tokenizer.Caret;
                        var nextToken = tokenizer.PeekNext();
                    }
                }

                query.AddAttribute(Query.QueryAttribute.Batches, batches);

                return query;
            */
        }
    }
}
