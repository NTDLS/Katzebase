using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserJoin
    {
        public static List<QuerySchema> Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var result = new List<QuerySchema>();
            string token;

            while (tokenizer.TryEatIfNext("inner"))
            {
                if (tokenizer.TryEatIfNext("join") == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [join], found: [{tokenizer.EatGetNextEvaluated()}]");
                }

                string subSchemaSchema = tokenizer.EatGetNext();
                string subSchemaAlias = string.Empty;
                if (!TokenizerHelpers.IsValidIdentifier(subSchemaSchema, ':'))
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected schema name, found: [{tokenizer.ResolveLiteral(subSchemaSchema)}].");
                }

                if (tokenizer.TryEatIfNext("as"))
                {
                    subSchemaAlias = tokenizer.EatGetNext();
                }
                else
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [as] (schema alias), found: [{tokenizer.EatGetNextEvaluated()}].");
                }

                if (tokenizer.TryEatIfNext("on", out token) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [on], found: [{tokenizer.ResolveLiteral(token)}].");
                }

                int joinConditionsStartPosition = tokenizer.Caret;

                if (tokenizer.TryGetFirstIndexOf(["where", "order", "inner", "group"], out var joinConditionsEndPosition) == false)
                {
                    joinConditionsEndPosition = tokenizer.Length; //No end marker found, consume the entire query.
                }

                int joinConditionsLength = joinConditionsEndPosition - joinConditionsStartPosition;
                var joinConditionsText = tokenizer.Text.Substring(joinConditionsStartPosition, joinConditionsLength).Trim();

                tokenizer.Caret = joinConditionsStartPosition;

                var joinConditions = StaticConditionsParser.Parse(queryBatch, tokenizer, joinConditionsText, joinConditionsEndPosition, subSchemaAlias);

                result.Add(new QuerySchema(subSchemaSchema.ToLowerInvariant(), subSchemaAlias.ToLowerInvariant(), joinConditions));
            }

            return result;
        }
    }
}
