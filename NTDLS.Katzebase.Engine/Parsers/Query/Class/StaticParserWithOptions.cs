using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes.PreparedQuery;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserWithOptions
    {
        /// <summary>
        /// Parses "with options" and returns the dictionary of values that can be added to a prepared query.
        /// </summary>
        internal static Dictionary<QueryAttribute, object> Parse(Tokenizer tokenizer, ExpectedWithOptions expectedOptions)
        {
            var results = new Dictionary<QueryAttribute, object>();

            if (tokenizer.TryIsNextCharacter('(') == false)
            {
                throw new KbParserException("Invalid query. Found '" + tokenizer.NextCharacter + "', expected: '('.");
            }
            tokenizer.EatNextCharacter();

            while (!tokenizer.IsExhausted())
            {
                string name = tokenizer.EatGetNext().ToLowerInvariant();
                if (tokenizer.TryIsNextCharacter('=') == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.NextCharacter + "', expected: '='.");
                }
                tokenizer.EatNextCharacter();

                string? tokenValue = tokenizer.EatGetNext().ToLowerInvariant();

                if (expectedOptions.ContainsKey(name) == false)
                {
                    var expectedValues = "'" + string.Join("','", expectedOptions.Select(o => o.Key)) + "'";
                    throw new KbParserException($"Invalid query. Found '{name}', expected {expectedValues}.");
                }

                if (tokenizer.Literals.TryGetValue(tokenValue, out var literal))
                {
                    tokenValue = literal.Value;
                }

                var convertedValue = expectedOptions.ValidateAndConvert(name, tokenValue);

                var option = new WithOption(name, convertedValue, convertedValue.GetType());
                if (Enum.TryParse(option.Name, true, out QueryAttribute optionType) == false)
                {
                    var expectedValues = "'" + string.Join("','", expectedOptions.Select(o => o.Key)) + "'";
                    throw new KbParserException($"Invalid query. Found '{option.Name}', expected {expectedValues}.");
                }

                results.Add(optionType, option.Value);

                if (tokenizer.TryEatIfNextCharacter(',') == false)
                {
                    break;
                }
            }

            if (tokenizer.TryIsNextCharacter(')') == false)
            {
                throw new KbParserException("Invalid query. Found '" + tokenizer.NextCharacter + "', expected: ')'.");
            }
            tokenizer.EatNextCharacter();

            return results;
        }
    }
}
