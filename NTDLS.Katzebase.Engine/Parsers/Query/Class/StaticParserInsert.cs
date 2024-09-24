using fs;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserInsert
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Insert);

            tokenizer.EatIfNext("into");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException("Invalid query. Found '" + schemaName + "', expected: schema name.");
            }
            query.Schemas.Add(new QuerySchema(schemaName));

            tokenizer.IsNext('(');
            query.InsertFieldNames = tokenizer.EatGetMatchingScope().Split(',').Select(o => o.Trim()).ToList();

            if (tokenizer.TryEatIfNext("values"))
            {
                //We have a values list.

                query.InsertFieldValues = new List<QueryFieldCollection>();

                while (!tokenizer.IsExhausted())
                {
                    tokenizer.IsNext('(');

                    var constantExpressions = tokenizer.EatGetMatchingScope().Split(',')
                        .Select(o => new QueryFieldCollapsedValue(queryBatch.GetLiteralValue(o.Trim()) ?? fstring.SEmpty)).ToList();

                    if (constantExpressions.Count < query.InsertFieldNames.Count)
                    {
                        throw new KbParserException("Values list contains less values than the field list.");
                    }
                    else if (constantExpressions.Count > query.InsertFieldNames.Count)
                    {
                        throw new KbParserException("Values list contains more values than the field list.");
                    }

                    var queryFieldCollection = new QueryFieldCollection(queryBatch);

                    foreach (var value in constantExpressions)
                    {
                        queryFieldCollection.Add(new QueryField(query.InsertFieldNames[queryFieldCollection.Count], queryFieldCollection.Count, value));
                    }

                    query.InsertFieldValues.Add(queryFieldCollection);

                    if (tokenizer.TryEatIfNext(',') == false)
                    {
                        //We are done parsing the values list.
                        break;
                    }
                }
            }
            else if (tokenizer.TryEatIfNext("select"))
            {
                query.InsertSelectQuery = StaticParserSelect.Parse(queryBatch, tokenizer);
            }
            else
            {
                throw new KbParserException($"Invalid token, found [{tokenizer.GetNext()}], expected [values], [select]");
            }

            return query;
        }
    }
}
