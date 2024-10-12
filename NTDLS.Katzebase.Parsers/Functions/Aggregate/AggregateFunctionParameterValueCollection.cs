using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;

namespace NTDLS.Katzebase.Parsers.Functions.Aggregate
{
    public class AggregateFunctionParameterValueCollection
    {
        public List<AggregateFunctionParameterValue> Values { get; private set; } = new();

        public T? Get<T>(string name)
        {
            try
            {
                var parameter = Values.FirstOrDefault(o => o.Parameter.Name.Is(name))
                    ?? throw new KbGenericException($"Value for [{name}] cannot be null.");

                if (parameter.Value == null)
                {
                    if (parameter.Parameter.HasDefault == false)
                    {
                        throw new KbGenericException($"Value for [{name}] cannot be null.");
                    }
                    return Converters.ConvertToNullable<T?>(parameter.Parameter.DefaultValue);
                }

                return Converters.ConvertToNullable<T?>(parameter.Value);
            }
            catch (Exception ex)
            {
                throw new KbGenericException($"Error parsing function parameter [{name}]. {ex.Message}");
            }
        }
    }
}
