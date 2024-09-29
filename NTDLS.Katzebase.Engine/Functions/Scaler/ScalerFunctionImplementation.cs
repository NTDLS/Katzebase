using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Scaler.Implementations;

namespace NTDLS.Katzebase.Engine.Functions.Scaler
{
    /// <summary>
    /// Contains all scaler function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    internal class ScalerFunctionImplementation
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
                "String Coalesce (StringInfinite text)|'Returns the first  non null of the given values.'",
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

        public static string? ExecuteFunction(Transaction transaction, string functionName, List<string?> parameters, KbInsensitiveDictionary<string?> rowValues)
        {
            var function = ScalerFunctionCollection.ApplyFunctionPrototype(functionName, parameters);

            return functionName.ToLowerInvariant() switch
            {
                "isbetween" => ScalerIsBetween.Execute(transaction, function),
                "isequal" => ScalerIsEqual.Execute(transaction, function),
                "isgreater" => ScalerIsGreater.Execute(transaction, function),
                "isgreaterorequal" => ScalerIsGreaterOrEqual.Execute(transaction, function),
                "isless" => ScalerIsLess.Execute(transaction, function),
                "islessorequal" => ScalerIsLessOrEqual.Execute(transaction, function),
                "islike" => ScalerIsLike.Execute(transaction, function),
                "isnotbetween" => ScalerIsNotBetween.Execute(transaction, function),
                "isnotequal" => ScalerIsNotEqual.Execute(transaction, function),
                "isinteger" => ScalerIsInteger.Execute(function),
                "isstring" => ScalerIsString.Execute(function),
                "isdouble" => ScalerIsDouble.Execute(function),
                "isnotlike" => ScalerIsNotLike.Execute(transaction, function),
                "checksum" => ScalerChecksum.Execute(function),
                "lastindexof" => ScalerLastIndexOf.Execute(function),
                "length" => ScalerLength.Execute(function),
                "coalesce" => ScalerCoalesce.Execute(parameters),
                "concat" => ScalerConcat.Execute(parameters),
                "datetime" => ScalerDateTime.Execute(function),
                "datetimeutc" => ScalerDateTimeUTC.Execute(function),
                "documentid" => ScalerDocumentID.Execute(function, rowValues),
                "documentpage" => ScalerDocumentPage.Execute(function, rowValues),
                "documentuid" => ScalerDocumentUID.Execute(function, rowValues),
                "guid" => ScalerGuid.Execute(function),
                "iif" => ScalerIIF.Execute(function),
                "indexof" => ScalerIndexOf.Execute(function),
                "left" => ScalerLeft.Execute(function),
                "right" => ScalerRight.Execute(function),
                "sha1" => ScalerSha1.Execute(function),
                "sha256" => ScalerSha256.Execute(function),
                "sha512" => ScalerSha512.Execute(function),
                "substring" => ScalerSubString.Execute(function),
                "tolower" => ScalerToLower.Execute(function),
                "toproper" => ScalerToProper.Execute(function),
                "toupper" => ScalerToUpper.Execute(function),
                "trim" => ScalerTrim.Execute(function),

                _ => throw new KbParserException($"The scaler function is not implemented: [{functionName}].")
            };
        }
    }
}
