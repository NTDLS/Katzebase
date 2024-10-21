using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Conditions;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Parsing
{
    public static class StaticParserWhere
    {
        public static ConditionCollection Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var endOfConditionsCaret = tokenizer.FindEndOfQuerySegment([" group ", " order ", " offset ", " inner ", " outer "]);
            string conditionText = tokenizer.SubStringAbsolute(endOfConditionsCaret).Trim();
            if (string.IsNullOrWhiteSpace(conditionText))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected conditions, found: [{conditionText}].");
            }

            try
            {
                tokenizer.PushSyntheticLimit(endOfConditionsCaret);
                return StaticConditionsParser.Parse(queryBatch, tokenizer, conditionText, endOfConditionsCaret);
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
    }
}
