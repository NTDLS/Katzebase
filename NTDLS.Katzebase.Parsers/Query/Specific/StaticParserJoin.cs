using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Query.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserJoin
    {
        public static List<QuerySchema> Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var result = new List<QuerySchema>();

            while (tokenizer.TryEatIfNext("inner"))
            {
                if (tokenizer.TryEatIfNext("join") == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [join], found: [{tokenizer.EatGetNextResolved()}]");
                }

                var schemaScriptLine = tokenizer.GetCurrentLineNumber();

                string subSchemaSchema = tokenizer.EatGetNext();
                string subSchemaAlias;
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
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [as] (schema alias), found: [{tokenizer.EatGetNextResolved()}].");
                }

                if (tokenizer.TryEatIfNext("on", out var onToken) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [on], found: [{tokenizer.ResolveLiteral(onToken)}].");
                }

                var endOfJoinCaret = tokenizer.FindEndOfQuerySegment([" where ", " order ", " inner ", " offset ", " group "]);
                string joinConditionsText = tokenizer.SubStringAbsolute(endOfJoinCaret).Trim();
                if (string.IsNullOrEmpty(joinConditionsText))
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected join conditions, found: [{joinConditionsText}].");
                }

                try
                {
                    tokenizer.PushSyntheticLimit(endOfJoinCaret);
                    var joinConditions = StaticConditionsParser.Parse(queryBatch, tokenizer, joinConditionsText, endOfJoinCaret.EnsureNotNull(), subSchemaAlias);
                    result.Add(new QuerySchema(schemaScriptLine, subSchemaSchema, QuerySchemaUsageType.InnerJoin, subSchemaAlias.ToLowerInvariant(), joinConditions));
                }
                catch
                {
                    throw;
                }
                finally
                {
                    tokenizer.PopSyntheticLimit();
                    tokenizer.EatWhiteSpace();
                }
            }

            return result;
        }
    }
}
