﻿using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserUpdate
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            /*Example query:
             * update
	         *       Test
             *   set
	         *       FirstName = 'Jane',
	         *       MiddleName = Guid(),
	         *       LastName = 'Doe'
             *   where
	         *       Id = 10
             */

            var query = new PreparedQuery(queryBatch, QueryType.Update);

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException($"Invalid query. Found [{schemaName}], expected: schema name.");
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
                    throw new KbParserException($"Invalid query. Found [{fieldName}], expected: field name.");
                }
                query.UpdateFieldNames.Add(fieldName);

                tokenizer.EatIfNext('=');

                bool isTextRemaining = tokenizer.EatGetSingleFieldExpression(["where", "inner"], out var fieldExpression);

                var queryField = StaticParserField.Parse(tokenizer, fieldExpression, query.UpdateFieldValues);

                query.UpdateFieldValues.Add(new QueryField(fieldName, query.UpdateFieldValues.Count, queryField));

                if (!isTextRemaining)
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