using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.Query.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserSelect
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Select, tokenizer.GetCurrentLineNumber());

            //Parse "TOP n".
            if (tokenizer.TryEatIfNext("top"))
            {
                query.RowLimit = tokenizer.EatGetNextEvaluated<int>();
            }

            //Parse field list.
            if (tokenizer.TryEatIfNext("*"))
            {
                //Select all fields from all schemas.
                query.DynamicSchemaFieldFilter ??= new();
            }
            else if (tokenizer.TryEatNextEndsWith(".*", out var starSchema)) //schemaName.*
            {
                //Select all fields from given schema.
                //TODO: Looks like do we not support "select *" from than one schema, probably never will.
                query.DynamicSchemaFieldFilter ??= new();
                var starSchemaAlias = starSchema[..^2]; //Trim off the trailing .*
                query.DynamicSchemaFieldFilter.Add(starSchemaAlias.ToLowerInvariant());
            }
            else
            {
                query.SelectFields = StaticParserSelectFields.Parse(queryBatch, tokenizer);
            }

            //Parse "into".
            if (tokenizer.TryEatIfNext("into"))
            {
                if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var selectIntoSchema) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{tokenizer.ResolveLiteral(selectIntoSchema)}].");
                }

                query.AddAttribute(PreparedQuery.QueryAttribute.TargetSchema, selectIntoSchema);

                query.QueryType = QueryType.SelectInto;
            }

            //Parse primary schema.
            if (!tokenizer.TryEatIfNext("from"))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [from], found: [{tokenizer.EatGetNextEvaluated()}].");
            }

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{tokenizer.ResolveLiteral(schemaName)}].");
            }

            if (tokenizer.TryEatIfNext("as"))
            {
                var schemaAlias = tokenizer.EatGetNext();
                query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName.ToLowerInvariant(), QuerySchemaUsageType.Primary, schemaAlias.ToLowerInvariant()));
            }
            else
            {
                query.Schemas.Add(new QuerySchema(tokenizer.GetCurrentLineNumber(), schemaName.ToLowerInvariant(), QuerySchemaUsageType.Primary));
            }

            //Parse joins.
            while (tokenizer.TryIsNext("inner"))
            {
                var joinedSchemas = StaticParserJoin.Parse(queryBatch, tokenizer);
                query.Schemas.AddRange(joinedSchemas);
            }

            //Parse "where" clause.
            if (tokenizer.TryEatIfNext("where"))
            {
                query.Conditions = StaticParserWhere.Parse(queryBatch, tokenizer);

                //Associate the root query schema with the root conditions.
                query.Schemas.First().Conditions = query.Conditions;
            }

            //Parse "group by".
            if (tokenizer.TryEatIfNext("group"))
            {
                if (tokenizer.TryEatIfNext("by") == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [by], found: [{tokenizer.EatGetNextEvaluated()}].");
                }
                query.GroupBy = StaticParserGroupBy.Parse(queryBatch, tokenizer);
            }

            //Parse "order by".
            if (tokenizer.TryEatIfNext("order"))
            {
                if (tokenizer.TryEatIfNext("by") == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [by], found: [{tokenizer.EatGetNextEvaluated()}].");
                }
                query.OrderBy = StaticParserOrderBy.Parse(queryBatch, tokenizer);
            }

            //Parse "limit" clause.
            if (tokenizer.TryEatIfNext("offset"))
            {
                query.RowOffset = tokenizer.EatGetNextEvaluated<int>();
            }

            return query;
        }
    }
}
