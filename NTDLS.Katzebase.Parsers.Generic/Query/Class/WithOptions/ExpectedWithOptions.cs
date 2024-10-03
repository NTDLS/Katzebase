using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Parsers.Tokens;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Parsers.Query.Class.WithOptions
{
    public class ExpectedWithOptions<TData> : KbInsensitiveDictionary<Type> where TData : IStringable
    {
        public ExpectedWithOptions()
        {
        }

        public object ValidateAndConvert(Tokenizer<TData> tokenizer, string name, string? value)
        {
            if (TryGetValue(name, out var resultType))
            {
                try
                {
                    if (resultType.BaseType?.Name.Is("enum") == true)
                    {
                        if (Enum.TryParse(resultType, value, true, out var enumValue) == false)
                        {
                            throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Invalid value passed to with option: [{name}].");
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
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Invalid NULL value passed to with option: [{name}].");
                    }
                    return resultingValue;
                }
                catch
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Failed to convert with option [{name}] value to [{resultType.Name}].");
                }
            }
            throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Invalid with option: [{name}].");
        }
    }
}
