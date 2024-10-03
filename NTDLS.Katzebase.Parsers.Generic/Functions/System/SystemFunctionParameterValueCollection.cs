using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Parsers.Functions.System
{
    public class SystemFunctionParameterValueCollection<TData> where TData : IStringable
    {
        public List<SystemFunctionParameterValue<TData>> Values { get; private set; } = new();

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
                    //return Converters.ConvertTo<T>(parameter.Parameter.DefaultValue);
                    return parameter.Parameter.DefaultValue.ToT<T>();
                }

                //return Converters.ConvertTo<T>(parameter.Value);
                return parameter.Value.ToT<T>();
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
                var value = Values.FirstOrDefault(o => o.Parameter.Name.Is(name)).Value;
                if (value == null)
                {
                    return defaultValue;
                }

                //return Converters.ConvertTo<T>(value);
                return value.ToT<T>();
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
                var value = Values.FirstOrDefault(o => o.Parameter.Name.Is(name)).Value;
                //return Converters.ConvertToNullable<T?>(value);
                return value.ToNullableT<T?>();
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
                var value = Values.FirstOrDefault(o => o.Parameter.Name.Is(name)).Value;
                if (value == null)
                {
                    return defaultValue;
                }

                //return Converters.ConvertToNullable<T?>(value);
                return value.ToNullableT<T?>();
            }
            catch (Exception ex)
            {
                throw new KbGenericException($"Error parsing function parameter [{name}]. {ex.Message}");
            }
        }
    }
}
