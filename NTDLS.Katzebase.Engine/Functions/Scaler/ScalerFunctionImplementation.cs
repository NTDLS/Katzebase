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
                "Boolean IsBetween (Numeric value, Numeric rangeLow, Numeric rangeHigh)",
                "Boolean IsEqual (String text1, String text2)",
                "Boolean IsGreater (Numeric value1, Numeric value2)",
                "Boolean IsGreaterOrEqual (Numeric value1, Numeric value2)",
                "Boolean IsLess (Numeric value1, Numeric value2)",
                "Boolean IsLessOrEqual (Numeric value1, Numeric value2)",
                "Boolean IsLike (String text, String pattern)",
                "Boolean IsNotBetween (Numeric value, Numeric rangeLow, Numeric rangeHigh)",
                "Boolean IsNotEqual (String text1, String text2)",
                "Boolean IsNotLike (String text, String pattern)",
                "Boolean IsInteger (String value)",
                "Boolean IsString (String value)",
                "Boolean IsDouble (String value)",
                "Numeric Checksum (String text)",
                "Numeric LastIndexOf (String textToFind, String textToSearch)",
                "Numeric Length (String text)",
                "String Coalesce (StringInfinite text)",
                "String Concat (StringInfinite text)",
                "String DateTime (String format = 'yyyy-MM-dd HH:mm:ss.ms')",
                "String DateTimeUTC (String format = 'yyyy-MM-dd HH:mm:ss.ms')",
                "String DocumentID (String schemaAlias)",
                "String DocumentPage (String schemaAlias)",
                "String DocumentUID (String schemaAlias)",
                "String Guid ()",
                "String IIF (Boolean condition, String whenTrue, String whenFalse)",
                "String IndexOf (String textToFind, String textToSearch)",
                "String Left (String text, Numeric length)",
                "String Right (String text, Numeric length)",
                "String Sha1 (String text)",
                "String Sha256 (String text)",
                "String Sha512 (String text)",
                "String SubString (String text, Numeric startIndex, Numeric length)",
                "String ToLower (String text)",
                "String ToProper (String text)",
                "String ToUpper (String text)",
                "String Trim  (String text)",
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
                "isinteger" => ScalerIsInteger.Execute( function),
                "isstring" => ScalerIsString.Execute( function),
                "isdouble" => ScalerIsDouble.Execute( function),
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
