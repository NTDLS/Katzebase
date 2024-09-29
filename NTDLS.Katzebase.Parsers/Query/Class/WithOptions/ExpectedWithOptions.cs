using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Parsers.Query.Class.WithOptions
{
    public class ExpectedWithOptions : KbInsensitiveDictionary<Type>
    {
        public ExpectedWithOptions()
        {
        }

        public object ValidateAndConvert(string name, string? value)
        {
            if (TryGetValue(name, out var resultType))
            {
                try
                {
                    if (resultType.BaseType?.Name.Is("enum") == true)
                    {
                        if (Enum.TryParse(resultType, value, true, out var enumValue) == false)
                        {
                            throw new KbParserException($"Invalid value passed to with option: [{name}].");
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
                        throw new KbParserException($"Invalid NULL value passed to with option: [{name}].");
                    }
                    return resultingValue;
                }
                catch
                {
                    throw new KbParserException($"Failed to convert with option [{name}] value to [{resultType.Name}].");
                }
            }
            throw new KbParserException($"Invalid with option: [{name}].");
        }
    }
}
