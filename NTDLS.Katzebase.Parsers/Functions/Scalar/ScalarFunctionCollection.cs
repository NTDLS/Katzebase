using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Parsers.Functions.Scalar
{
    public static class ScalarFunctionCollection
    {
        internal static string[] PrototypeStrings = {
                //Prototype Format: "returnDataType functionName (parameterDataType parameterName, parameterDataType parameterName = defaultValue)"
                //Parameters that have a default specified are considered optional, they should come after non-optional parameters.
                "Boolean IsBetween (Numeric value, Numeric rangeLow, Numeric rangeHigh)|'Returns true if the value is within the given range.'",
                "Boolean IsEqual (String text1, String text2)|'Returns true if the given values are equal.'",
                "Boolean IsGreater (Numeric value1, Numeric value2)|'Returns true if the first given value is greater than the second.'",
                "Boolean IsGreaterOrEqual (Numeric value1, Numeric value2)|'Returns true if the first given value is equal to or greater than the second.'",
                "Boolean IsLess (Numeric value1, Numeric value2)|'Returns true if the first given value is less than the second.'",
                "Boolean IsLessOrEqual (Numeric value1, Numeric value2)|'Returns true if the first given value is equal to or less than the second.'",
                "Boolean IsLike (String text, String pattern)|'Returns true if the value matches the pattern.'",
                "Boolean IsNotBetween (Numeric value, Numeric rangeLow, Numeric rangeHigh)|'Returns true if the value is not within the given range.'",
                "Boolean IsNotEqual (String text1, String text2)|'Returns true if the given values are not equal.'",
                "Boolean IsNotLike (String text, String pattern)|'Returns true if the value does not match the pattern.'",
                "Boolean IsInteger (String value)|'Returns true if the given value is a not decimal integer.'",
                "Boolean IsString (String value)|'Returns true if the given value cannot be converted to a numeric.'",
                "Boolean IsDouble (String value)|'Returns true if the given value can be converted to a numeric decimal.'",
                "Numeric Checksum (String text)|'Returns a numeric CRC32 for the given value.'",
                "Numeric LastIndexOf (String textToFind, String textToSearch, Numeric offset = -1)|'Returns the zero based index for the last occurrence of the second given value in the first value.'",
                "Numeric Length (String text)|'Returns the length of the given value.'",
                "String Coalesce (StringInfinite text)|'Returns the first non null of the given values.'",
                "String Concat (StringInfinite text)|'Concatenates all non null of the given values.'",
                "String DateTime (String format = 'yyyy-MM-dd HH:mm:ss.ms')|'Returns the current local date and time using the given format.'",
                "String DateTimeUTC (String format = 'yyyy-MM-dd HH:mm:ss.ms')|'Returns the current UCT date and time using the given format.'",
                "String DocumentID (String schemaAlias)|'Returns the ID of the current document.'",
                "String DocumentPage (String schemaAlias)|'Returns the page number of the current document.'",
                "String DocumentUID (String schemaAlias)|'Returns the page number and ID of the current document.'",
                "String Guid ()|'Returns a random globally unique identifier.'",
                "String IIF (Boolean condition, String whenTrue, String whenFalse)|'Conditionally returns second of third given values based on whether the first parameter can be evaluated to true.'",
                "String IndexOf (String textToFind, String textToSearch, Numeric offset = 0)|'Returns the zero based index of the start of textToSearch in textToFind.'",
                "String Left (String text, Numeric length)|'Returns the specified number of character from the left hand side of the given value.'",
                "String Right (String text, Numeric length)|'Returns the specified number of character from the right hand side of the given value.'",
                "String Sha1 (String text)|'Returns a SHA1 hash for the given value.'",
                "String Sha256 (String text)|'Returns a SHA256 hash for the given value.'",
                "String Sha512 (String text)|'Returns a numeric SHA512 hash for the given value.'",
                "String SubString (String text, Numeric startIndex, Numeric length)|'Returns a range of characters from the given value based on the startIndex and length.'",
                "String ToLower (String text)|'Returns the lower cased variant of the given value.'",
                "String ToProper (String text)|'Returns the best effort proper cased variant of the given value, capitolizing the first character in each word.'",
                "String ToUpper (String text)|'Returns the upper cased variant of the given value.'",
                "String Trim (String text, String characters = null)|'Returns the given value with white space stripped from the beginning and end, optionally specifying the character to trim.'",
            };

        private static List<ScalarFunction>? _protypes = null;
        public static List<ScalarFunction> Prototypes
        {
            get
            {
                if (_protypes == null)
                {
                    throw new KbFatalException("Function prototypes were not initialized.");
                }
                return _protypes;
            }
        }

        public static void Initialize()
        {
            if (_protypes == null)
            {
                _protypes = new();

                foreach (var prototype in PrototypeStrings)
                {
                    _protypes.Add(ScalarFunction.Parse(prototype));
                }
            }
        }

        public static bool TryGetFunction(string name, [NotNullWhen(true)] out ScalarFunction? function)
        {
            function = Prototypes.FirstOrDefault(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return function != null;
        }

        public static ScalarFunctionParameterValueCollection ApplyFunctionPrototype(string functionName, List<string?> parameters)
        {
            if (_protypes == null)
            {
                throw new KbFatalException("Function prototypes were not initialized.");
            }

            var function = _protypes.FirstOrDefault(o => o.Name.Is(functionName))
                ?? throw new KbFunctionException($"Undefined scalar function: [{functionName}].");

            return function.ApplyParameters(parameters);
        }
    }
}
