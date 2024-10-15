using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;

namespace NTDLS.Katzebase.Parsers.Functions.Scalar
{
    public class ScalarFunctionParameterValueCollection
    {
        public List<ScalarFunctionParameterValue> Values { get; private set; } = new();

        public T? Get<T>(string name)
        {
            try
            {
                var parameter = Values.FirstOrDefault(o => o.Parameter.Name.Is(name))
                    ?? throw new KbGenericException($"Value is not defined: [{name}].");

                return Converters.ConvertToNullable<T?>(parameter.Value);
            }
            catch (Exception ex)
            {
                throw new KbGenericException($"Error parsing function parameter [{name}]. {ex.Message}");
            }
        }
    }
}
