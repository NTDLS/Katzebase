using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserInsert<TData> where TData : IStringable
    {
        /*Example (ragged key/value pair):
         * insert into Test
         * (FirstName = 'Jane', LastName = 'Doe'),
         * (FirstName = 'John', MiddleName = Guid(), LastName = 'Doe')
        */

        /*Example (field list / value list):
         * insert into Test(FirstName, LastName)
         * values('Jane', 'Doe'),('John', 'Doe'),('Test', Guid())
        */

        enum FieldParserType
        {
            None,
            KeyValue,
            ValueListPossibleSelectFrom
        }

        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer, Func<string, TData> parseStringToDoc, Func<string, TData> castStringToDoc)
        {
            var query = new PreparedQuery<TData>(queryBatch, QueryType.Insert);

            tokenizer.EatIfNext("into");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{schemaName}], expected: schema name.");
            }
            query.Schemas.Add(new QuerySchema<TData>(schemaName));

            var fieldParserType = FieldParserType.None;

            //Test to figure out what kind of insert statement we have
            int firstParenthesesCaret = tokenizer.Caret;

            tokenizer.EatIfNext('(');
            tokenizer.EatGetNext(); //Skip the fieldName.

            if (tokenizer.TryIsNext(','))
            {
                fieldParserType = FieldParserType.ValueListPossibleSelectFrom;
            }
            else if (tokenizer.TryIsNext('='))
            {
                fieldParserType = FieldParserType.KeyValue;
            }
            else
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{tokenizer.NextCharacter}], expected: [,] or [=].");
            }
            tokenizer.SetCaret(firstParenthesesCaret);

            if (fieldParserType == FieldParserType.KeyValue)
            {
                query.InsertFieldValues = new List<QueryFieldCollection<TData>>();

                while (!tokenizer.IsExhausted())
                {
                    tokenizer.EatIfNext('('); //Beginning of key/values set.

                    var queryFieldCollection = new QueryFieldCollection<TData>(queryBatch);

                    while (!tokenizer.IsExhausted())
                    {
                        var fieldName = tokenizer.EatGetNext();
                        tokenizer.EatIfNext('=');
                        bool isTextRemaining = tokenizer.EatGetSingleFieldExpression([")"], out var fieldExpression);

                        var queryField = StaticParserField<TData>.Parse(tokenizer, fieldExpression, queryFieldCollection, parseStringToDoc, castStringToDoc);

                        queryFieldCollection.Add(new QueryField<TData>(fieldName, queryFieldCollection.Count, queryField));

                        if (isTextRemaining == false)
                        {
                            break;
                        }
                    }

                    tokenizer.EatIfNext(')'); //End of key/values set.

                    query.InsertFieldValues.Add(queryFieldCollection);

                    if (tokenizer.TryEatIfNext(',') == false)
                    {
                        break;
                    }
                }

            }
            else if (fieldParserType == FieldParserType.ValueListPossibleSelectFrom)
            {
                query.InsertFieldNames = tokenizer.EatGetMatchingScope().Split(',').Select(o => o.Trim()).ToList();

                if (tokenizer.TryEatIfNext("values"))
                {
                    //We have a values list.

                    query.InsertFieldValues = new List<QueryFieldCollection<TData>>();

                    while (!tokenizer.IsExhausted())
                    {
                        tokenizer.EatIfNext('('); //Beginning of key/values set.

                        var queryFieldCollection = new QueryFieldCollection<TData>(queryBatch);

                        while (!tokenizer.IsExhausted())
                        {
                            bool isTextRemaining = tokenizer.EatGetSingleFieldExpression([")"], out var fieldExpression);

                            var queryField = StaticParserField<TData>.Parse(tokenizer, fieldExpression, queryFieldCollection, parseStringToDoc, castStringToDoc);

                            queryFieldCollection.Add(new QueryField<TData>(query.InsertFieldNames[queryFieldCollection.Count], queryFieldCollection.Count, queryField));

                            if (isTextRemaining == false)
                            {
                                break;
                            }
                        }

                        tokenizer.EatIfNext(')'); //End of key/values set.

                        if (queryFieldCollection.Count < query.InsertFieldNames.Count)
                        {
                            throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Values list contains less values than the field list.");
                        }
                        else if (queryFieldCollection.Count > query.InsertFieldNames.Count)
                        {
                            throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Values list contains more values than the field list.");
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
                    query.InsertSelectQuery = StaticParserSelect<TData>.Parse(queryBatch, tokenizer, parseStringToDoc: parseStringToDoc, castStringToDoc: castStringToDoc);
                }
                else
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Invalid token, found [{tokenizer.GetNext()}], expected [values], [select]");
                }
            }
            else
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Invalid query, field list or key/value insert statement was malformed.");
            }

            return query;
        }
    }
}
