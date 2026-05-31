using NTDLS.Katzebase.Parsers.Conditions;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarIsNumeric
    {
        //"Boolean IsNumeric (String value = null)|'Returns true if the given value can be converted to a numeric.'",
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var text = function.Get<string?>("text");
            if (string.IsNullOrEmpty(text))
                return "false";

            int i = 0;

            // Optional leading sign
            if (text[i] == '+' || text[i] == '-')
                i++;

            if (i == text.Length)
                return "false";

            bool hasDecimal = false;
            bool hasDigit = false;

            for (; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= '0' && c <= '9')
                {
                    hasDigit = true;
                }
                else if (c == '.' && !hasDecimal)
                {
                    hasDecimal = true;
                }
                else
                {
                    return "false";
                }
            }

            return hasDigit ? "true" : "false";
        }
    }
}
