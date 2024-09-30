﻿using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Class.WithOptions;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Query.SupportingTypes.PreparedQuery;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserWithOptions
    {
        /// <summary>
        /// Parses "with options" and returns the dictionary of values that can be added to a prepared query.
        /// </summary>
        internal static Dictionary<QueryAttribute, object> Parse(Tokenizer tokenizer, ExpectedWithOptions expectedOptions)
        {
            var results = new Dictionary<QueryAttribute, object>();

            if (tokenizer.TryIsNextCharacter('(') == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{tokenizer.NextCharacter}], expected: [(].");
            }
            tokenizer.EatNextCharacter();

            while (!tokenizer.IsExhausted())
            {
                string name = tokenizer.EatGetNext().ToLowerInvariant();
                if (tokenizer.TryIsNextCharacter('=') == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{tokenizer.NextCharacter}], expected: [=].");
                }
                tokenizer.EatNextCharacter();

                string? tokenValue = tokenizer.EatGetNext().ToLowerInvariant();

                if (expectedOptions.ContainsKey(name) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{name}], expected [{string.Join("],[", expectedOptions.Select(o => o.Key))}].");
                }

                if (tokenizer.Literals.TryGetValue(tokenValue, out var literal))
                {
                    tokenValue = literal.Value;
                }

                var convertedValue = expectedOptions.ValidateAndConvert(tokenizer, name, tokenValue);

                var option = new WithOption(name, convertedValue, convertedValue.GetType());
                if (Enum.TryParse(option.Name, true, out QueryAttribute optionType) == false)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{option.Name}], expected [{string.Join("],[", expectedOptions.Select(o => o.Key))}].");
                }

                results.Add(optionType, option.Value);

                if (tokenizer.TryEatIfNextCharacter(',') == false)
                {
                    break;
                }
            }

            if (tokenizer.TryIsNextCharacter(')') == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{tokenizer.NextCharacter}], expected: [)].");
            }
            tokenizer.EatNextCharacter();

            return results;
        }
    }
}
