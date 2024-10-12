using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarFormatDateTime
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var datetimeString = function.Get<string?>("datetime");
            if (datetimeString == null)
            {
                return null;
            }

            if (DateTime.TryParse(datetimeString, out var datetime) == false)
            {
                throw new KbProcessingException($"Expected a valid date-time expression, found: [{datetimeString}].");
            }

            return datetime.ToString(function.Get<string>("format"));
        }
    }
}
