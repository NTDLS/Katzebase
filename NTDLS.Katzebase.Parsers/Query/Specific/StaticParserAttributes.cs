using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserAttributes
    {
        /// <summary>
        /// Parses "with options" and returns the dictionary of values that can be added to a prepared query.
        /// </summary>
        internal static KbInsensitiveDictionary<QueryAttribute> Parse(Tokenizer tokenizer, ExpectedQueryAttributes expectedOptions)
        {
            var results = new KbInsensitiveDictionary<QueryAttribute>();

            if (tokenizer.TryIsNextCharacter('(') == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [(], found: [{tokenizer.NextCharacter}].");
            }
            tokenizer.EatNextCharacter();

            while (!tokenizer.IsExhausted())
            {
                string attributeName = tokenizer.EatGetNext();
                if (tokenizer.TryIsNextCharacter('=') == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [=], found: [{tokenizer.NextCharacter}].");
                }
                tokenizer.EatNextCharacter();

                string? attributeValue = tokenizer.EatGetNext();

                if (expectedOptions.TryGetValue(attributeName, out var matchedOptionType) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(),
                        $"Expected [{string.Join("],[", expectedOptions.Select(o => o.Key))}], found: [{tokenizer.ResolveLiteral(attributeName)}]");
                }

                if (tokenizer.Literals.TryGetValue(attributeValue, out var literal))
                {
                    attributeValue = literal.Value;
                }

                var convertedValue = expectedOptions.ValidateAndConvert(tokenizer, attributeName, attributeValue);
                var option = new QueryAttribute(attributeName, convertedValue, convertedValue.GetType());
                results.Add(attributeName, option);

                if (tokenizer.TryEatIfNextCharacter(',') == false)
                {
                    break;
                }
            }

            if (tokenizer.TryIsNextCharacter(')') == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected: [)], found: [{tokenizer.NextCharacter}].");
            }
            tokenizer.EatNextCharacter();

            return results;
        }
    }
}
