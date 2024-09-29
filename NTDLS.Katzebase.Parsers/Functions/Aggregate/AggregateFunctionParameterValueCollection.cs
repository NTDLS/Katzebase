using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Parsers.Functions.Aggregate
{
    public class AggregateFunctionParameterValueCollection
    {
        public List<AggregateFunctionParameterValue> Values { get; private set; } = new();

        public T Get<T>(string name)
        {
            try
            {
                var parameter = Values.FirstOrDefault(o => o.Parameter.Name.Is(name))
                    ?? throw new KbGenericException($"Value for [{name}] cannot be null.");

                if (parameter.Value == null)
                {
                    if (parameter.Parameter.DefaultValue == null)
                    {
                        throw new KbGenericException($"Value for [{name}] cannot be null.");
                    }
                    return Converters.ConvertTo<T>(parameter.Parameter.DefaultValue);
                }

                return Converters.ConvertTo<T>(parameter.Value);
            }
            catch (Exception ex)
            {
                throw new KbGenericException($"Error parsing function parameter [{name}]. {ex.Message}");
            }
        }

        public T Get<T>(string name, T defaultValue)
        {
            try
            {
                var value = Values.FirstOrDefault(o => o.Parameter.Name.Is(name))?.Value;
                if (value == null)
                {
                    return defaultValue;
                }

                return Converters.ConvertTo<T>(value);
            }
            catch (Exception ex)
            {
                throw new KbGenericException($"Error parsing function parameter [{name}]. {ex.Message}");
            }
        }

        public T? GetNullable<T>(string name)
        {
            try
            {
                var parameter = Values.FirstOrDefault(o => o.Parameter.Name.Is(name))
                    ?? throw new KbGenericException($"Value for [{name}] cannot be null.");

                if (parameter.Value == null)
                {
                    if (parameter.Parameter.DefaultValue == null)
                    {
                        throw new KbGenericException($"Value for [{name}] cannot be null.");
                    }
                    return Converters.ConvertToNullable<T>(parameter.Parameter.DefaultValue);
                }

                return Converters.ConvertToNullable<T>(parameter.Value);
            }
            catch (Exception ex)
            {
                throw new KbGenericException($"Error parsing function parameter [{name}]. {ex.Message}");
            }
        }

        public T? GetNullable<T>(string name, T? defaultValue)
        {
            try
            {
                var value = Values.FirstOrDefault(o => o.Parameter.Name.Is(name))?.Value;
                if (value == null)
                {
                    return defaultValue;
                }

                return Converters.ConvertToNullable<T>(value);
            }
            catch (Exception ex)
            {
                throw new KbGenericException($"Error parsing function parameter [{name}]. {ex.Message}");
            }
        }
    }
}
