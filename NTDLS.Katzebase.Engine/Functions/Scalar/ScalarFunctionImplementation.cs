using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
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
                "abs" => ScalarAbs.Execute(function),
                "ceil" => ScalarCeil.Execute(function),
                "checksum" => ScalarChecksum.Execute(function),
                "coalesce" => ScalarCoalesce.Execute(parameters),
                "concat" => ScalarConcat.Execute(parameters),
                "dateadd" => ScalarDateAdd.Execute(function),
                "datediff" => ScalarDateDiff.Execute(function),
                "datetime" => ScalarDateTime.Execute(function),
                "datetimeutc" => ScalarDateTimeUTC.Execute(function),
                "documentid" => ScalarDocumentID.Execute(function, rowValues),
                "documentpage" => ScalarDocumentPage.Execute(function, rowValues),
                "documentuid" => ScalarDocumentUID.Execute(function, rowValues),
                "floor" => ScalarFloor.Execute(function),
                "formatdatetime" => ScalarFormatDateTime.Execute(function),
                "formatnumeric" => ScalarFormatNumeric.Execute(function),
                "guid" => ScalarGuid.Execute(function),
                "ifnull" => ScalarIfNull.Execute(function),
                "ifnullnumeric" => ScalarIfNullNumeric.Execute(function),
                "iif" => ScalarIIF.Execute(function),
                "indexof" => ScalarIndexOf.Execute(function),
                "isbetween" => ScalarIsBetween.Execute(transaction, function),
                "isdouble" => ScalarIsDouble.Execute(function),
                "isequal" => ScalarIsEqual.Execute(transaction, function),
                "isgreater" => ScalarIsGreater.Execute(transaction, function),
                "isgreaterorequal" => ScalarIsGreaterOrEqual.Execute(transaction, function),
                "isinteger" => ScalarIsInteger.Execute(function),
                "isless" => ScalarIsLess.Execute(transaction, function),
                "islessorequal" => ScalarIsLessOrEqual.Execute(transaction, function),
                "islike" => ScalarIsLike.Execute(transaction, function),
                "isnotbetween" => ScalarIsNotBetween.Execute(transaction, function),
                "isnotequal" => ScalarIsNotEqual.Execute(transaction, function),
                "isnotlike" => ScalarIsNotLike.Execute(transaction, function),
                "isnull" => ScalarIsNull.Execute(function),
                "isstring" => ScalarIsString.Execute(function),
                "lastindexof" => ScalarLastIndexOf.Execute(function),
                "left" => ScalarLeft.Execute(function),
                "length" => ScalarLength.Execute(function),
                "nullif" => ScalarNullIf.Execute(function),
                "nullifnumeric" => ScalarNullIfNumeric.Execute(function),
                "pow" => ScalarPow.Execute(function),
                "right" => ScalarRight.Execute(function),
                "round" => ScalarRound.Execute(function),
                "sha1" => ScalarSha1.Execute(function),
                "sha256" => ScalarSha256.Execute(function),
                "sha512" => ScalarSha512.Execute(function),
                "substring" => ScalarSubString.Execute(function),
                "tolower" => ScalarToLower.Execute(function),
                "tonumeric" => ScalarToNumeric.Execute(function),
                "toproper" => ScalarToProper.Execute(function),
                "tostring" => ScalarToString.Execute(function),
                "toupper" => ScalarToUpper.Execute(function),
                "trim" => ScalarTrim.Execute(function),

                _ => throw new KbNotImplementedException($"The scalar function is not implemented: [{functionName}].")
            };
        }
    }
}
