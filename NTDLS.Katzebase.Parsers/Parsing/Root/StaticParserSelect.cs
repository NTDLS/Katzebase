﻿using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Parsing.Root
{
    public static class StaticParserSelect
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Select, tokenizer.GetCurrentLineNumber());

            //Parse "pre-TOP distinct".
            if (tokenizer.TryEatIfNext("distinct"))
            {
                query.PreTopDistinct = true;
            }

            //Parse "TOP n".
            if (tokenizer.TryEatIfNext("top"))
            {
                query.RowLimit = tokenizer.EatGetNextResolved<int>();
            }

            //Parse "post-TOP distinct".
            if (tokenizer.TryEatIfNext("distinct"))
            {
                if (query.PreTopDistinct)
                {
                    throw new KbParserException("Expected field list, found: [distinct].");
                }

                query.PostTopDistinct = true;
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
                if (tokenizer.TryEatValidateNext((o) => o.IsIdentifier(), out var selectIntoSchema) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{tokenizer.Variables.Resolve(selectIntoSchema)}].");
                }

                query.AddAttribute(PreparedQuery.Attribute.TargetSchemaName, selectIntoSchema);

                query.QueryType = QueryType.SelectInto;
            }

            //Parse primary schema.
            if (!tokenizer.TryEatIfNext("from"))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [from], found: [{tokenizer.EatGetNextResolved()}].");
            }

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
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [by], found: [{tokenizer.EatGetNextResolved()}].");
                }
                query.GroupBy = StaticParserGroupBy.Parse(queryBatch, tokenizer);
            }

            //Parse "order by".
            if (tokenizer.TryEatIfNext("order"))
            {
                if (tokenizer.TryEatIfNext("by") == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [by], found: [{tokenizer.EatGetNextResolved()}].");
                }
                query.OrderBy = StaticParserOrderBy.Parse(queryBatch, tokenizer);
            }

            //Parse "limit" clause.
            if (tokenizer.TryEatIfNext("offset"))
            {
                query.RowOffset = tokenizer.EatGetNextResolved<int>();
            }

            return query;
        }
    }
}
