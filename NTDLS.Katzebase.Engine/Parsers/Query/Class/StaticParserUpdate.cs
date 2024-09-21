using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using NTDLS.Katzebase.Shared;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserUpdate
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Update);

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException("Invalid query. Found '" + schemaName + "', expected: schema name.");
            }
            if (tokenizer.TryEatIfNext("as"))
            {
                var schemaAlias = tokenizer.EatGetNext();
                query.Schemas.Add(new QuerySchema(schemaName.ToLowerInvariant(), schemaAlias.ToLowerInvariant()));
            }
            else
            {
                query.Schemas.Add(new QuerySchema(schemaName.ToLowerInvariant(), schemaName.ToLowerInvariant()));
            }
            tokenizer.EatIfNext("set");

            query.UpdateFieldValues = new QueryFieldCollection(queryBatch);

            while (!tokenizer.IsExhausted())
            {
                if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var fieldName) == false)
                {
                    throw new KbParserException("Invalid query. Found '" + fieldName + "', expected: field name.");
                }
                query.UpdateFieldNames.Add(fieldName);

                tokenizer.EatIfNext('=');

                string token;
                int startCaret = tokenizer.Caret;
                int endCaret = 0;

                while (!tokenizer.IsExhausted())
                {
                    token = tokenizer.GetNext();
                    if (token == "(")
                    {
                        tokenizer.EatMatchingScope();
                    }
                    else if (token.Is("where") || token.Is("inner"))
                    {
                        endCaret = tokenizer.Caret;
                        break; //exit loop to parse, found where or join clause.
                    }
                    else if (token.Length == 1 && token[0] == ',')
                    {
                        endCaret = tokenizer.Caret;
                        tokenizer.EatNext();
                        break; //exit loop to parse next field.
                    }
                    else if (token.Length == 1 && (token[0].IsTokenConnectorCharacter() || token[0].IsMathematicalOperator()))
                    {
                        tokenizer.EatNext();
                    }
                    else
                    {
                        tokenizer.EatNext();
                    }
                }

                var fieldValue = tokenizer.Substring(startCaret, endCaret - startCaret).Trim();
                var queryField = StaticParserField.Parse(tokenizer, fieldValue, query.UpdateFieldValues);

                query.UpdateFieldValues.Add(new QueryField(fieldName, query.UpdateFieldValues.Count, queryField));

                if (tokenizer.TryIsNext(["where", "inner"]))
                {
                    break; //exit loop to parse, found where or join clause.
                }
            }

            if (tokenizer.TryEatIfNext("where"))
            {
                query.Conditions = StaticParserWhere.Parse(queryBatch, tokenizer);

                //Associate the root query schema with the root conditions.
                query.Schemas.First().Conditions = query.Conditions;
            }

            return query;
        }
    }
}
