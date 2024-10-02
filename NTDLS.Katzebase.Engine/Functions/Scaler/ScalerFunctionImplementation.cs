using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Scaler.Implementations;
using NTDLS.Katzebase.Parsers.Functions.Scaler;

namespace NTDLS.Katzebase.Engine.Functions.Scaler
{
    /// <summary>
    /// Contains all scaler function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    internal class ScalerFunctionImplementation
    {
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

                _ => throw new KbNotImplementedException($"The scaler function is not implemented: [{functionName}].")
            };
        }
    }
}
