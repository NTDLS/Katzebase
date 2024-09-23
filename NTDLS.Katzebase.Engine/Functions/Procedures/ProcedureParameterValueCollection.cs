using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.Engine.Functions.Procedures
{
    internal class ProcedureParameterValueCollection
    {
        public List<ProcedureParameterValue> Values { get; private set; } = new();

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

                if (typeof(T) == typeof(string) || (Nullable.GetUnderlyingType(typeof(T)) == typeof(string)))
                {
                    return Converters.ConvertTo<T>(parameter.Value.Substring(1, parameter.Value.Length - 2));
                }

                return Converters.ConvertTo<T>(parameter.Value);
            }
            catch
            {
                throw new KbGenericException($"Undefined parameter: [{name}].");
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

                if (typeof(T) == typeof(string) || (Nullable.GetUnderlyingType(typeof(T)) == typeof(string)))
                {
                    return Converters.ConvertTo<T>(value.Substring(1, value.Length - 2));
                }

                return Converters.ConvertTo<T>(value);
            }
            catch
            {
                throw new KbGenericException($"Undefined parameter: [{name}].");
            }
        }

        public T? GetNullable<T>(string name)
        {
            var parameter = Values.FirstOrDefault(o => o.Parameter.Name.Is(name))
                ?? throw new KbGenericException($"Value for [{name}] cannot be null.");

            if (parameter.Value == null)
            {
                if (parameter.Parameter.HasDefault == false)
                {
                    throw new KbGenericException($"Value for [{name}] is not optional.");
                }
                return Converters.ConvertToNullable<T>(parameter.Parameter.DefaultValue);
            }

            if (typeof(T) == typeof(string) || (Nullable.GetUnderlyingType(typeof(T)) == typeof(string)))
            {
                return Converters.ConvertToNullable<T>(parameter.Value.Substring(1, parameter.Value.Length - 2));
            }

            return Converters.ConvertToNullable<T>(parameter.Value);
        }

        public T? GetNullable<T>(string name, T? defaultValue)
        {
            var value = Values.FirstOrDefault(o => o.Parameter.Name.Is(name))?.Value;
            if (value == null)
            {
                return defaultValue;
            }

            if (typeof(T) == typeof(string) || (Nullable.GetUnderlyingType(typeof(T)) == typeof(string)))
            {
                return Converters.ConvertToNullable<T>(value.Substring(1, value.Length - 2));
            }

            return Converters.ConvertToNullable<T>(value);
        }
    }
}
