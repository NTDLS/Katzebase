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

        public static string? ExecuteFunction(Transaction transaction, string functionName, List<string?> parameters, KbInsensitiveDictionary<string?> rowFields)
        {
            var function = ScalerFunctionCollection.ApplyFunctionPrototype(functionName, parameters);

            return functionName.ToLowerInvariant() switch
            {
                "isbetween" => ScalerIsBetween.Execute(function),
                "isequal" => ScalerIsEqual.Execute(function),
                "isgreater" => ScalerIsGreater.Execute(function),
                "isgreaterorequal" => ScalerIsGreaterOrEq.Execute(function),
                "isless" => ScalerIsLess.Execute(function),
                "islessorequal" => ScalerIsLessOrEqual.Execute(function),
                "islike" => ScalerIsLike.Execute(function),
                "isnotbetween" => ScalerIsNotBetween.Execute(function),
                "isnotequal" => ScalerIsNotEqual.Execute(function),
                "isnotlike" => ScalerIsNotLike.Execute(function),
                "checksum" => ScalerChecksum.Execute(function),
                "lastindexof" => ScalerLastIndexOf.Execute(function),
                "length" => ScalerLength.Execute(function),
                "coalesce" => ScalerCoalesce.Execute(function),
                "concat" => ScalerConcat.Execute(function),
                "datetime" => ScalerDateTime.Execute(function),
                "datetimeutc" => ScalerDateTimeUTC.Execute(function),
                "documentid" => ScalerDocumentID.Execute(function),
                "documentpage" => ScalerDocumentPage.Execute(function),
                "documentuid" => ScalerDocumentUID.Execute(function),
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
