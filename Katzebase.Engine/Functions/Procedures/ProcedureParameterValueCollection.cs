using Katzebase.Engine.Library;
using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Functions.Procedures
{
    internal class ProcedureParameterValueCollection
    {
        public List<ProcedureParameterValue> Values { get; set; } = new();

        public T Get<T>(string name)
        {
            try
            {
                var parameter = Values.Where(o => o.Parameter.Name.ToLower() == name.ToLower()).FirstOrDefault();
                if (parameter == null)
                {
                    throw new KbGenericException($"Value for {name} cannot be null.");
                }

                if (parameter.Value == null)
                {
                    if (parameter.Parameter.DefaultValue == null)
                    {
                        throw new KbGenericException($"Value for {name} cannot be null.");
                    }
                    return Helpers.ConvertTo<T>(parameter.Parameter.DefaultValue);
                }

                if (typeof(T) == typeof(string) || (Nullable.GetUnderlyingType(typeof(T)) == typeof(string)))
                {
                    return Helpers.ConvertTo<T>(parameter.Value.Substring(1, parameter.Value.Length - 2));
                }

                return Helpers.ConvertTo<T>(parameter.Value);
            }
            catch
            {
                throw new KbGenericException($"Undefined parameter {name}.");
            }
        }

        public T Get<T>(string name, T defaultValue)
        {
            try
            {
                var value = Values.Where(o => o.Parameter.Name.ToLower() == name.ToLower()).FirstOrDefault()?.Value;
                if (value == null)
                {
                    return defaultValue;
                }

                if (typeof(T) == typeof(string) || (Nullable.GetUnderlyingType(typeof(T)) == typeof(string)))
                {
                    return Helpers.ConvertTo<T>(value.Substring(1, value.Length - 2));
                }

                return Helpers.ConvertTo<T>(value);
            }
            catch
            {
                throw new KbGenericException($"Undefined parameter {name}.");
            }
        }

        public T? GetNullable<T>(string name)
        {
            var parameter = Values.Where(o => o.Parameter.Name.ToLower() == name.ToLower()).FirstOrDefault();
            if (parameter == null)
            {
                throw new KbGenericException($"Value for {name} cannot be null.");
            }

            if (parameter.Value == null)
            {
                if (parameter.Parameter.HasDefault == false)
                {
                    throw new KbGenericException($"Value for {name} is not optional.");
                }
                return Helpers.ConvertToNullable<T>(parameter.Parameter.DefaultValue);
            }

            if (typeof(T) == typeof(string) || (Nullable.GetUnderlyingType(typeof(T)) == typeof(string)))
            {
                return Helpers.ConvertToNullable<T>(parameter.Value.Substring(1, parameter.Value.Length - 2));
            }

            return Helpers.ConvertToNullable<T>(parameter.Value);
        }

        public T? GetNullable<T>(string name, T? defaultValue)
        {
            var value = Values.Where(o => o.Parameter.Name.ToLower() == name.ToLower()).FirstOrDefault()?.Value;
            if (value == null)
            {
                return defaultValue;
            }

            if (typeof(T) == typeof(string) || (Nullable.GetUnderlyingType(typeof(T)) == typeof(string)))
            {
                return Helpers.ConvertToNullable<T>(value.Substring(1, value.Length - 2));
            }

            return Helpers.ConvertToNullable<T>(value);
        }
    }
}
