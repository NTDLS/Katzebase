using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.Class.WithOptions
{
    public class ExpectedWithOptions : KbInsensitiveDictionary<Type>
    {
        public ExpectedWithOptions()
        {
        }

        public object ValidateAndConvert(Tokenizer tokenizer, string name, string? value)
        {
            if (TryGetValue(name, out var resultType))
            {
                try
                {
                    if (resultType.BaseType?.Name.Is("enum") == true)
                    {
                        if (Enum.TryParse(resultType, value, true, out var enumValue) == false)
                        {
                            throw new KbParserException(tokenizer.GetCurrentLineNumber(),
                                $"Expected: [{string.Join("],[", Enum.GetValues(resultType))}], found: [{name}].");
                        }
                        return Convert.ChangeType(enumValue, resultType);
                    }
                    else if (resultType.Name.Is("boolean"))
                    {
                        if (double.TryParse(value, out var boolValue))
                        {
                            return boolValue != 0;
                        }

                        return value?.Is("true") == true;
                    }

                    var resultingValue = Convert.ChangeType(value, resultType);
                    if (resultingValue == null)
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected option, found: null.");
                    }
                    return resultingValue;
                }
                catch
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Failed to convert [{resultType.Name}] option, found: [{name}].");
                }
            }
            throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Unexpected with option, found: [{name}].");
        }
    }
}
