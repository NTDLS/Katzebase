using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    /// <summary>
    /// Contains the name and type of expected query attributes. Used for validating while parsing
    /// </summary>
    public class ExpectedQueryAttributes : KbInsensitiveDictionary<Type>
    {
        public ExpectedQueryAttributes()
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

                        if (value?.Is("true") == true)
                        {
                            return true;
                        }
                        else if (value?.Is("false") == true)
                        {
                            return false;
                        }
                    }

                    var resultingValue = Convert.ChangeType(value, resultType);
                    return resultingValue == null
                        ? throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected option, found: null.")
                        : resultingValue;
                }
                catch
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Failed to convert [{resultType.Name}] option for [{name}], found: [{value}].");
                }
            }
            throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Unexpected with option, found: [{name}].");
        }
    }
}
