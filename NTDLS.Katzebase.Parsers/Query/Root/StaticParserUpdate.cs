using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Query.Root
{
    public static class StaticParserUpdate
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
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

            var query = new PreparedQuery(queryBatch, QueryType.Update, tokenizer.GetCurrentLineNumber());

            if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var updateSchemaNameOrAlias) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name or alias, found: [{tokenizer.Variables.Resolve(updateSchemaNameOrAlias)}].");
            }

            tokenizer.EatIfNext("set");

            query.UpdateFieldValues = new QueryFieldCollection(queryBatch);

            /*
            var endOfConditionsCaret = tokenizer.FindEndOfQuerySegment([" where ", " inner "," outer ", " from "]);
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
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected field name, found: [{tokenizer.Variables.Resolve(fieldName)}].");
                }
                query.UpdateFieldNames.Add(fieldName);

                tokenizer.EatIfNext('=');

                bool isTextRemaining = tokenizer.EatGetSingleFieldExpression(["where", "inner", "outer", "from"], out var fieldExpression);

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
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{tokenizer.Variables.Resolve(schemaName)}].");
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
                while (tokenizer.TryIsNext(["inner", "outer"]))
                {
                    var joinedSchemas = StaticParserJoin.Parse(queryBatch, tokenizer);
                    query.Schemas.AddRange(joinedSchemas);
                }

                var targetSchema = query.Schemas.Where(o => o.Alias.Is(updateSchemaNameOrAlias)).FirstOrDefault()
                    ?? throw new KbParserException(query.ScriptLine, $"Update schema now found in query: [{updateSchemaNameOrAlias}].");

                query.AddAttribute(PreparedQuery.Attribute.TargetSchemaAlias, targetSchema.Alias);
            }
            else
            {
                //The query did not have a from, so the schema specified on the UPDATE line is the schema name.
                query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), updateSchemaNameOrAlias.ToLowerInvariant(), QuerySchemaUsageType.Primary));
                query.AddAttribute(PreparedQuery.Attribute.TargetSchemaAlias, string.Empty);
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
