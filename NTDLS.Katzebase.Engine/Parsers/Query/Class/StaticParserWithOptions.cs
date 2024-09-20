using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserWithOptions
    {
        internal static List<WithOption> Parse(Tokenizer tokenizer, ExpectedWithOptions expectedOptions, PreparedQuery preparedQuery)
        {
            var results = new List<WithOption>();

            if (tokenizer.TryEatIfNext("with"))
            {
                if (tokenizer.TryIsNextCharacter('(') == false)
                {
                    throw new KbParserException("Invalid query. Found '" + tokenizer.NextCharacter + "', expected: '('.");
                }
                tokenizer.EatNextCharacter();

                while (!tokenizer.IsExhausted())
                {
                    string name = tokenizer.GetNext().ToLowerInvariant();
                    if (tokenizer.TryIsNextCharacter('=') == false)
                    {
                        throw new KbParserException("Invalid query. Found '" + tokenizer.NextCharacter + "', expected: '='.");
                    }
                    tokenizer.EatNextCharacter();

                    string? tokenValue = tokenizer.GetNext().ToLowerInvariant();

                    if (expectedOptions.ContainsKey(name) == false)
                    {
                        var expectedValues = "'" + string.Join("','", expectedOptions.Select(o => o.Key)) + "'";
                        throw new KbParserException($"Invalid query. Found '{name}', expected {expectedValues}.");
                    }

                    if (tokenizer.Literals.TryGetValue(tokenValue, out var literal))
                    {
                        tokenValue = literal.Value;
                        tokenValue = tokenValue == null ? null : tokenValue.Substring(1, tokenValue.Length - 2);
                    }

                    var convertedValue = expectedOptions.ValidateAndConvert(name, tokenValue);

                    results.Add(new WithOption(name, convertedValue, convertedValue.GetType()));

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
            }

            foreach (var option in results)
            {
                if (Enum.TryParse(option.Name, true, out PreparedQuery.QueryAttribute optionType) == false)
                {
                    var expectedValues = "'" + string.Join("','", expectedOptions.Select(o => o.Key)) + "'";
                    throw new KbParserException($"Invalid query. Found '{option.Name}', expected {expectedValues}.");
                }
                preparedQuery.AddAttribute(optionType, option.Value);
            }

            return results;
        }
    }
}
