using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;


namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserWithOptions<TData> where TData : IStringable
    {
        /// <summary>
        /// Parses "with options" and returns the dictionary of values that can be added to a prepared query.
        /// </summary>
        internal static Dictionary<PreparedQuery<TData>.QueryAttribute, object> Parse<TData>(Tokenizer<TData> tokenizer, ExpectedWithOptions expectedOptions)
            where TData : IStringable
        {
            var results = new Dictionary<PreparedQuery<TData>.QueryAttribute, object>();

            if (tokenizer.TryIsNextCharacter('(') == false)
            {
                throw new KbParserException($"Invalid query. Found [{tokenizer.NextCharacter}], expected: [(].");
            }
            tokenizer.EatNextCharacter();

            while (!tokenizer.IsExhausted())
            {
                string name = tokenizer.EatGetNext().ToLowerInvariant();
                if (tokenizer.TryIsNextCharacter('=') == false)
                {
                    throw new KbParserException($"Invalid query. Found [{tokenizer.NextCharacter}], expected: [=].");
                }
                tokenizer.EatNextCharacter();

                string? tokenValue = tokenizer.EatGetNext().ToLowerInvariant();

                if (expectedOptions.ContainsKey(name) == false)
                {
                    throw new KbParserException($"Invalid query. Found [{name}], expected [{string.Join("],[", expectedOptions.Select(o => o.Key))}].");
                }

                if (tokenizer.Literals.TryGetValue(tokenValue, out var literal))
                {
                    tokenValue = literal.Value.ToT<string>();
                }

                var convertedValue = expectedOptions.ValidateAndConvert(name, tokenValue);

                var option = new WithOption(name, convertedValue, convertedValue.GetType());
                if (Enum.TryParse(option.Name, true, out PreparedQuery<TData>.QueryAttribute optionType) == false)
                {
                    throw new KbParserException($"Invalid query. Found [{option.Name}], expected [{string.Join("],[", expectedOptions.Select(o => o.Key))}].");
                }

                results.Add(optionType, option.Value);

                if (tokenizer.TryEatIfNextCharacter(',') == false)
                {
                    break;
                }
            }

            if (tokenizer.TryIsNextCharacter(')') == false)
            {
                throw new KbParserException($"Invalid query. Found [{tokenizer.NextCharacter}], expected: [)].");
            }
            tokenizer.EatNextCharacter();

            return results;
        }
    }
}
