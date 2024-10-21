using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Parsing.Root
{
    public static class StaticParserInsert
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

        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Insert, tokenizer.GetCurrentLineNumber());

            tokenizer.EatIfNext("into");

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{schemaName}].");
            }
            query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));
            query.AddAttribute(PreparedQuery.Attribute.TargetSchemaName, schemaName);

            var fieldParserType = FieldParserType.None;

            //Test to figure out what kind of insert statement we have
            int firstParenthesesCaret = tokenizer.Caret;

            tokenizer.EatIfNext('(');
            tokenizer.EatGetNext([':', ','], out var outStoppedOnDelimiter); //Skip the fieldName.

            if (outStoppedOnDelimiter == ',')
            {
                fieldParserType = FieldParserType.ValueListPossibleSelectFrom;
            }
            else if (outStoppedOnDelimiter == ':')
            {
                fieldParserType = FieldParserType.KeyValue;
            }
            else
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected: [,] or [:], found: [{outStoppedOnDelimiter}].");
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
                        var fieldName = tokenizer.EatGetNext([' ', ':'], out outStoppedOnDelimiter);

                        if (outStoppedOnDelimiter == ' ')
                        {
                            //Parsing: "FirstName : 'Value'"
                            tokenizer.EatIfNext(':');
                        }
                        else
                        {
                            //Parsing: "FirstName:'Value'"
                        }

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
                            throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Values list contains less values than field list.");
                        }
                        else if (queryFieldCollection.Count > query.InsertFieldNames.Count)
                        {
                            throw new KbParserException(tokenizer.GetCurrentLineNumber(), "Values list contains more values than field list.");
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
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [values], [select], found: [{tokenizer.EatGetNextResolved()}]");
                }
            }
            else
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Malformed field list or key/value insert statement.");
            }

            return query;
        }
    }
}
