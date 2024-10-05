using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Scalar.Implementations;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar
{
    /// <summary>
    /// Contains all scalar function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    internal class ScalarFunctionImplementation
    {
        public static string? ExecuteFunction(Transaction transaction, string functionName, List<string?> parameters, KbInsensitiveDictionary<string?> rowValues)
        {
            var function = ScalarFunctionCollection.ApplyFunctionPrototype(functionName, parameters);

            return functionName.ToLowerInvariant() switch
            {
                "isbetween" => ScalarIsBetween.Execute(transaction, function),
                "isequal" => ScalarIsEqual.Execute(transaction, function),
                "isgreater" => ScalarIsGreater.Execute(transaction, function),
                "isgreaterorequal" => ScalarIsGreaterOrEqual.Execute(transaction, function),
                "isless" => ScalarIsLess.Execute(transaction, function),
                "islessorequal" => ScalarIsLessOrEqual.Execute(transaction, function),
                "islike" => ScalarIsLike.Execute(transaction, function),
                "isnotbetween" => ScalarIsNotBetween.Execute(transaction, function),
                "isnotequal" => ScalarIsNotEqual.Execute(transaction, function),
                "isinteger" => ScalarIsInteger.Execute(function),
                "isstring" => ScalarIsString.Execute(function),
                "isdouble" => ScalarIsDouble.Execute(function),
                "isnotlike" => ScalarIsNotLike.Execute(transaction, function),
                "checksum" => ScalarChecksum.Execute(function),
                "lastindexof" => ScalarLastIndexOf.Execute(function),
                "length" => ScalarLength.Execute(function),
                "coalesce" => ScalarCoalesce.Execute(parameters),
                "concat" => ScalarConcat.Execute(parameters),
                "datetime" => ScalarDateTime.Execute(function),
                "datetimeutc" => ScalarDateTimeUTC.Execute(function),
                "documentid" => ScalarDocumentID.Execute(function, rowValues),
                "documentpage" => ScalarDocumentPage.Execute(function, rowValues),
                "documentuid" => ScalarDocumentUID.Execute(function, rowValues),
                "guid" => ScalarGuid.Execute(function),
                "iif" => ScalarIIF.Execute(function),
                "indexof" => ScalarIndexOf.Execute(function),
                "left" => ScalarLeft.Execute(function),
                "right" => ScalarRight.Execute(function),
                "sha1" => ScalarSha1.Execute(function),
                "sha256" => ScalarSha256.Execute(function),
                "sha512" => ScalarSha512.Execute(function),
                "substring" => ScalarSubString.Execute(function),
                "tolower" => ScalarToLower.Execute(function),
                "toproper" => ScalarToProper.Execute(function),
                "toupper" => ScalarToUpper.Execute(function),
                "trim" => ScalarTrim.Execute(function),

                _ => throw new KbNotImplementedException($"The scalar function is not implemented: [{functionName}].")
            };
        }
    }
}
