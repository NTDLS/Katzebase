using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Scaler.Implementations;
using NTDLS.Katzebase.Parsers.Functions.Scaler;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Functions.Scaler
{
    /// <summary>
    /// Contains all scaler function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    public class ScalerFunctionImplementation
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

        public static TData? ExecuteFunction<TData>(Transaction<TData> transaction, string functionName, List<TData?> parameters, KbInsensitiveDictionary<TData?> rowValues) where TData:IStringable
        {
            var function = ScalerFunctionCollection<TData>.ApplyFunctionPrototype(functionName, parameters);

            string? rtn = functionName.ToLowerInvariant() switch
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
                "isinteger" => ScalerIsInteger.Execute<TData>(function),
                "isstring" => ScalerIsString.Execute<TData>(function),
                "isdouble" => ScalerIsDouble.Execute<TData>(function),
                "isnotlike" => ScalerIsNotLike.Execute<TData>(transaction, function),
                "checksum" => ScalerChecksum.Execute<TData>(function),
                "lastindexof" => ScalerLastIndexOf.Execute<TData>(function),
                "length" => ScalerLength.Execute<TData>(function),
                "coalesce" => ScalerCoalesce.Execute<TData>(parameters)?.ToString(),
                "concat" => ScalerConcat.Execute<TData>(parameters),
                "datetime" => ScalerDateTime.Execute<TData>(function),
                "datetimeutc" => ScalerDateTimeUTC.Execute<TData>(function),
                "documentid" => ScalerDocumentID.Execute<TData>(function, rowValues),
                "documentpage" => ScalerDocumentPage.Execute<TData>(function, rowValues),
                "documentuid" => ScalerDocumentUID.Execute<TData>(function, rowValues).ToT<string>(),
                "guid" => ScalerGuid.Execute<TData>(function),
                "iif" => ScalerIIF.Execute<TData>(function),
                "indexof" => ScalerIndexOf.Execute<TData>(function),
                "left" => ScalerLeft.Execute<TData>(function),
                "right" => ScalerRight.Execute<TData>(function),
                "sha1" => ScalerSha1.Execute<TData>(function),
                "sha256" => ScalerSha256.Execute<TData>(function),
                "sha512" => ScalerSha512.Execute<TData>(function),
                "substring" => ScalerSubString.Execute<TData>(function),
                "tolower" => ScalerToLower.Execute<TData>(function),
                "toproper" => ScalerToProper.Execute<TData>(function),
                "toupper" => ScalerToUpper.Execute<TData>(function),
                "trim" => ScalerTrim.Execute<TData>(function),

                _ => throw new KbParserException($"The scaler function is not implemented: [{functionName}].")
            };
            return rtn.ParseToT(EngineCore<TData>.StrCast);
        }
    }
}
