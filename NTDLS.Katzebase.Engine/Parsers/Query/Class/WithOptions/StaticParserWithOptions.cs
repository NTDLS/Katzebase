using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Query.Tokenizers;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class.WithOptions
{
    internal static class StaticParserWithOptions
    {
        internal static List<WithOption> Parse(
            ref QueryTokenizer query, ExpectedWithOptions expectedOptions, ref PreparedQuery preparedQuery)
        {
            var results = new List<WithOption>();

            if (query.PeekNext().Is("with"))
            {
                query.SkipNext();

                if (query.IsNextCharacter('(') == false)
                {
                    throw new KbParserException("Invalid query. Found '" + query.CurrentChar() + "', expected: '('.");
                }
                query.SkipNextCharacter();

                while (true)
                {
                    string name = query.GetNext().ToLowerInvariant();
                    if (query.IsNextCharacter('=') == false)
                    {
                        throw new KbParserException("Invalid query. Found '" + query.CurrentChar() + "', expected: '='.");
                    }
                    query.SkipNextCharacter();

                    string tokenValue = query.GetNext().ToLowerInvariant();

                    if (expectedOptions.ContainsKey(name) == false)
                    {
                        var expectedValues = "'" + string.Join("','", expectedOptions.Select(o => o.Key)) + "'";
                        throw new KbParserException($"Invalid query. Found '{name}', expected {expectedValues}.");
                    }

                    if (query.StringLiterals.TryGetValue(tokenValue, out string? value))
                    {
                        tokenValue = value;
                        tokenValue = tokenValue.Substring(1, tokenValue.Length - 2);
                    }

                    var convertedValue = expectedOptions.ValidateAndConvert(name, tokenValue);

                    results.Add(new WithOption(name, convertedValue, convertedValue.GetType()));

                    if (query.IsNextCharacter(','))
                    {
                        query.SkipNextCharacter();
                    }
                    else
                    {
                        break;
                    }
                }

                if (query.IsNextCharacter(')') == false)
                {
                    throw new KbParserException("Invalid query. Found '" + query.CurrentChar() + "', expected: ')'.");
                }
                query.SkipNextCharacter();
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
