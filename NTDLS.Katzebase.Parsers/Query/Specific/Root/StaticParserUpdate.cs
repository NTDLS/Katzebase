﻿using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.Query.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Query.Specific.Root
{
    public static class StaticParserUpdate
    {
        internal static SupportingTypes.Query Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            /*Example query:
             * 
             * update
	         *       Test
             *   set
	         *       FirstName = 'Jane',
	         *       MiddleName = Guid(),
	         *       LastName = 'Doe'
             *   where
	         *       Id = 10
	         * ---------------------------------------
	         *  update
	         *       t
             *   set
	         *       TargetWordId = t1.Len
             *   from
	         *       test as t
             *   inner join test1 as t1
	         *       on t1.TargetWordId = t.TargetWordId
             *   where
	         *       TargetLanguage = 'Latin'
	         *  
             */

            var query = new SupportingTypes.Query(queryBatch, QueryType.Update, tokenizer.GetCurrentLineNumber());

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var updateSchemaNameOrAlias) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name or alias, found: [{tokenizer.ResolveLiteral(updateSchemaNameOrAlias)}].");
            }

            tokenizer.EatIfNext("set");

            query.UpdateFieldValues = new QueryFieldCollection(queryBatch);

            /*
            var endOfConditionsCaret = tokenizer.FindEndOfQuerySegment([" where ", " inner ", " from "]);
            string conditionText = tokenizer.SubStringAbsolute(endOfConditionsCaret).Trim();
            if (string.IsNullOrWhiteSpace(conditionText))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected conditions, found: [{conditionText}].");
            }
            */

            while (!tokenizer.IsExhausted())
            {
                if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var fieldName) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected field name, found: [{tokenizer.ResolveLiteral(fieldName)}].");
                }
                query.UpdateFieldNames.Add(fieldName);

                tokenizer.EatIfNext('=');

                bool isTextRemaining = tokenizer.EatGetSingleFieldExpression(["where", "inner", "from"], out var fieldExpression);

                var queryField = StaticParserField.Parse(tokenizer, fieldExpression, query.UpdateFieldValues);

                query.UpdateFieldValues.Add(new QueryField(fieldName, query.UpdateFieldValues.Count, queryField));

                if (!isTextRemaining)
                {
                    break; //exit loop to parse, found: where or join clause.
                }
            }

            //Parse primary schema, otherwise use updateSchemaNameOrAlias
            if (tokenizer.TryEatIfNext("from"))
            {
                if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var schemaName) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{tokenizer.ResolveLiteral(schemaName)}].");
                }

                if (tokenizer.TryEatIfNext("as"))
                {
                    var schemaAlias = tokenizer.EatGetNext();
                    query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary, schemaAlias.ToLowerInvariant()));
                }
                else
                {
                    query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName, QuerySchemaUsageType.Primary));
                }

                //Parse joins.
                while (tokenizer.TryIsNext("inner"))
                {
                    var joinedSchemas = StaticParserJoin.Parse(queryBatch, tokenizer);
                    query.Schemas.AddRange(joinedSchemas);
                }

                var targetSchema = query.Schemas.Where(o => o.Alias.Is(updateSchemaNameOrAlias)).FirstOrDefault()
                    ?? throw new KbParserException(query.ScriptLine, $"Update schema now found in query: [{updateSchemaNameOrAlias}].");

                query.AddAttribute(SupportingTypes.Query.Attribute.TargetSchemaAlias, targetSchema.Alias);
            }
            else
            {
                //The query did not have a from, so the schema specified on the UPDATE line is the schema name.
                query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), updateSchemaNameOrAlias.ToLowerInvariant(), QuerySchemaUsageType.Primary));
                query.AddAttribute(SupportingTypes.Query.Attribute.TargetSchemaAlias, string.Empty);
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
