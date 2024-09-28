using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserUpdate<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
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

            var query = new PreparedQuery<TData>(queryBatch, QueryType.Update);

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException($"Invalid query. Found [{schemaName}], expected: schema name.");
            }
            if (tokenizer.TryEatIfNext("as"))
            {
                var schemaAlias = tokenizer.EatGetNext();
                query.Schemas.Add(new QuerySchema<TData>(schemaName.ToLowerInvariant(), schemaAlias.ToLowerInvariant()));
            }
            else
            {
                query.Schemas.Add(new QuerySchema<TData>(schemaName.ToLowerInvariant(), schemaName.ToLowerInvariant()));
            }
            tokenizer.EatIfNext("set");

            query.UpdateFieldValues = new QueryFieldCollection<TData>(queryBatch);

            while (!tokenizer.IsExhausted())
            {
                if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var fieldName) == false)
                {
                    throw new KbParserException($"Invalid query. Found [{fieldName}], expected: field name.");
                }
                query.UpdateFieldNames.Add(fieldName);

                tokenizer.EatIfNext('=');

                bool isTextRemaining = tokenizer.EatGetSingleFieldExpression(["where", "inner"], out var fieldExpression);

                var queryField = StaticParserField<TData>.Parse(tokenizer, fieldExpression, query.UpdateFieldValues);

                query.UpdateFieldValues.Add(new QueryField<TData>(fieldName, query.UpdateFieldValues.Count, queryField));

                if (!isTextRemaining)
                {
                    break; //exit loop to parse, found where or join clause.
                }
            }

            if (tokenizer.TryEatIfNext("where"))
            {
                query.Conditions = StaticParserWhere<TData>.Parse(queryBatch, tokenizer);

                //Associate the root query schema with the root conditions.
                query.Schemas.First().Conditions = query.Conditions;
            }

            return query;
        }
    }
}
