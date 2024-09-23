using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserInsert
    {
        /*Example (ragged key/value pair):
         * insert into Test
         * (FirstName = 'Jane', LastName = 'Doe'),
         * (FirstName = 'John', MiddleName = Guid(), LastName = 'Doe')
        */

        /*Example (ragged field list / value list):
         * insert into Test(FirstName, LastName)
         * values('Jane', 'Doe'),('John', 'Doe'),('Test', Guid())
        */

        enum FieldParserType
        {
            None,
            KeyValue,
            ValueListPossibleSelectFrom
        }

        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Insert);

            tokenizer.EatIfNext("into");

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException("Invalid query. Found [" + schemaName + "], expected: schema name.");
            }
            query.Schemas.Add(new QuerySchema(schemaName));

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
                throw new KbParserException("Invalid query. Found [" + tokenizer.NextCharacter + "], expected: [,] or [=].");
            }
            tokenizer.SetCaret(firstParenthesesCaret);

            if (fieldParserType == FieldParserType.KeyValue)
            {
                query.InsertFieldValues = new List<QueryFieldCollection>();

                while (!tokenizer.IsExhausted())
                {
                    tokenizer.EatIfNext('('); //Beginning of key/values set.

                    var queryFieldCollection = new QueryFieldCollection(queryBatch);

                    while (!tokenizer.IsExhausted())
                    {
                        var fieldName = tokenizer.EatGetNext();
                        tokenizer.EatIfNext('=');
                        bool isTextRemaining = tokenizer.EatGetSingleFieldExpression([")"], out var fieldExpression);

                        var queryField = StaticParserField.Parse(tokenizer, fieldExpression, queryFieldCollection);

                        queryFieldCollection.Add(new QueryField(fieldName, queryFieldCollection.Count, queryField));

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

                    query.InsertFieldValues = new List<QueryFieldCollection>();

                    while (!tokenizer.IsExhausted())
                    {
                        tokenizer.EatIfNext('('); //Beginning of key/values set.

                        var queryFieldCollection = new QueryFieldCollection(queryBatch);

                        while (!tokenizer.IsExhausted())
                        {
                            bool isTextRemaining = tokenizer.EatGetSingleFieldExpression([")"], out var fieldExpression);

                            var queryField = StaticParserField.Parse(tokenizer, fieldExpression, queryFieldCollection);

                            queryFieldCollection.Add(new QueryField(query.InsertFieldNames[queryFieldCollection.Count], queryFieldCollection.Count, queryField));

                            if (isTextRemaining == false)
                            {
                                break;
                            }
                        }

                        tokenizer.EatIfNext(')'); //End of key/values set.

                        if (queryFieldCollection.Count < query.InsertFieldNames.Count)
                        {
                            throw new KbParserException("Values list contains less values than the field list.");
                        }
                        else if (queryFieldCollection.Count > query.InsertFieldNames.Count)
                        {
                            throw new KbParserException("Values list contains more values than the field list.");
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
            }
            else
            {
                throw new KbParserException($"Invalid query, field list or key/value insert statement was malformed.");
            }

            return query;
        }
    }
}
