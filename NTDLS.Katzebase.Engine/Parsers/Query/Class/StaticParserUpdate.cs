using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserUpdate
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token;

            throw new NotImplementedException("Reimplement this query type.");

            var query = new PreparedQuery(queryBatch, QueryType.Update);

            if (tokenizer.TryEatValidateNext((o) => TokenizerExtensions.IsIdentifier(o), out var schemaName) == false)
            {
                throw new KbParserException("Invalid query. Found '" + schemaName + "', expected: schema name.");
            }
            query.Schemas.Add(new QuerySchema(schemaName));

            tokenizer.EatIfNext("set");

            //result.UpdateValues = StaticFunctionParsers.ParseUpdateFields(tokenizer);
            //result.UpdateValues.RepopulateLiterals(tokenizer);

            throw new NotImplementedException();

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
