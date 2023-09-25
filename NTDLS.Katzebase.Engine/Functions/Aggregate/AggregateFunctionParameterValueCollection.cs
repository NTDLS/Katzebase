using NTDLS.Katzebase.Engine.Functions.Aggregate.Parameters;
using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Exceptions;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    internal class AggregateFunctionParameterValueCollection
    {
        public List<AggregateFunctionParameterValue> Values { get; set; } = new();

        public T Get<T>(string name)
        {
            try
            {
                /*
                if(is List<AggregateDecimalArrayParameter>)
                        {
                }
                */
                var parameter = Values.Where(o => o.Parameter.Name.ToLower() == name.ToLower()).FirstOrDefault();
                if (parameter == null)
                {
                    throw new KbGenericException($"Value for {name} cannot be null.");
                }

                var paramValue = string.Empty;

                if (parameter.Value is AggregateDecimalArrayParameter)
                {
                    if (typeof(T) == typeof(AggregateDecimalArrayParameter))
                    {
                        return (T)Convert.ChangeType(parameter.Value, typeof(T));
                    }
                    throw new KbEngineException("Requested type must be AggregateDecimalArrayParameter.");
                }
                else if (parameter.Value is AggregateSingleParameter)
                {
                    paramValue = ((AggregateSingleParameter)parameter.Value).Value;
                }

                if (paramValue == null)
                {
                    if (parameter.Parameter.DefaultValue == null)
                    {
                        throw new KbGenericException($"Value for {name} cannot be null.");
                    }
                    return Helpers.ConvertTo<T>(parameter.Parameter.DefaultValue);
                }

                return Helpers.ConvertTo<T>(paramValue);
            }
            catch
            {
                throw new KbGenericException($"Undefined parameter {name}.");
            }
        }

        /*
        public T Get<T>(string name, T defaultValue)
        {
            try
            {
                var value = Values.Where(o => o.Parameter.Name.ToLower() == name.ToLower()).FirstOrDefault()?.Value;
                if (value == null)
                {
                    return defaultValue;
                }

                return Helpers.ConvertTo<T>(value);
            }
            catch
            {
                throw new KbGenericException($"Undefined parameter {name}.");
            }
        }
        */
        /*
        public T? GetNullable<T>(string name)
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
                    return Helpers.ConvertToNullable<T>(parameter.Parameter.DefaultValue);
                }

                return Helpers.ConvertToNullable<T>(parameter.Value);
            }
            catch
            {
                throw new KbGenericException($"Undefined parameter {name}.");
            }
        }
        */
        /*
        public T? GetNullable<T>(string name, T? defaultValue)
        {
            try
            {
                var value = Values.Where(o => o.Parameter.Name.ToLower() == name.ToLower()).FirstOrDefault()?.Value;
                if (value == null)
                {
                    return defaultValue;
                }

                return Helpers.ConvertToNullable<T>(value);
            }
            catch
            {
                throw new KbGenericException($"Undefined parameter {name}.");
            }
        }
        */
    }
}
