using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Query.Tokenizers;

namespace NTDLS.Katzebase.Engine.Query
{
    internal class WithOption
    {
        public string Name { get; private set; }
        public object Value { get; private set; }
        public Type ValueType { get; private set; }

        public WithOption(string name, object value, Type valueType)
        {
            Name = name;
            Value = value;
            ValueType = valueType;
        }
    }

    internal class ExpectedWithOptions : KbInsensitiveDictionary<Type>
    {
        public ExpectedWithOptions()
        {
        }

        public object ValidateAndConvert(string name, string value)
        {
            if (TryGetValue(name, out var resultType))
            {
                try
                {
                    if (string.Equals(resultType.BaseType?.Name, "enum", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (Enum.TryParse(resultType, value, true, out var enumValue) == false)
                        {
                            throw new KbParserException($"Invalid value passed to with option '{name}'.");
                        }
                        return Convert.ChangeType(enumValue, resultType);
                    }

                    var resultingValue = Convert.ChangeType(value, resultType);
                    if (resultingValue == null)
                    {
                        throw new KbParserException($"Invalid NULL value passed to with option '{name}'.");
                    }
                    return resultingValue;
                }
                catch
                {
                    throw new KbParserException($"Failed to convert with option '{name}' value to '{resultType.Name}'.");
                }
            }
            throw new KbParserException($"Invalid with option '{name}'.");
        }
    }

    internal static class StaticWithOptionsParser
    {
        internal static List<WithOption> ParseWithOptions(
            ref QueryTokenizer query, ExpectedWithOptions expectedOptions, ref PreparedQuery preparedQuery)
        {
            var results = new List<WithOption>();

            if (query.IsNextToken("with"))
            {
                query.SkipNextToken();

                if (query.IsNextCharacter('(') == false)
                {
                    throw new KbParserException("Invalid query. Found '" + query.CurrentChar() + "', expected: '('.");
                }
                query.SkipNextCharacter();

                while (true)
                {
                    string name = query.GetNextToken().ToLowerInvariant();
                    if (query.IsNextCharacter('=') == false)
                    {
                        throw new KbParserException("Invalid query. Found '" + query.CurrentChar() + "', expected: '='.");
                    }
                    query.SkipNextCharacter();

                    string value = query.GetNextToken().ToLowerInvariant();

                    if (expectedOptions.ContainsKey(name) == false)
                    {
                        var expectedValues = "'" + string.Join("','", expectedOptions.Select(o => o.Key)) + "'";
                        throw new KbParserException($"Invalid query. Found '{name}', expected {expectedValues}.");
                    }

                    if (query.LiteralStrings.ContainsKey(value))
                    {
                        value = query.LiteralStrings[value];
                        value = value.Substring(1, value.Length - 2);
                    }

                    var convertedValue = expectedOptions.ValidateAndConvert(name, value);

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
