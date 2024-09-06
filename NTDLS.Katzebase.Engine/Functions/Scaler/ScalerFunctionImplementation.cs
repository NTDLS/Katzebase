using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;
using System.Globalization;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

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
                "String Guid()",
                "String IIF (Boolean condition, String whenTrue, String whenFalse)",
                "String IndexOf (String textToFind, String textToSearch)",
                "String Left (String text, Numeric length)",
                "String Right (String text, Numeric length)",
                "String Sha1 (String text)",
                "String Sha256  (String text)",
                "String SubString (String text, Numeric startIndex, Numeric length)",
                "String ToLower (String text)",
                "String ToProper (String text)",
                "String ToUpper (String text)",
                "String Trim  (String text)",
            };

        public static string? ExecuteFunction(Transaction transaction, string functionName, List<string> parameters, KbInsensitiveDictionary<string?> rowFields)
        {
            var proc = ScalerFunctionCollection.ApplyFunctionPrototype(functionName, parameters);

            switch (functionName.ToLowerInvariant())
            {
                case "documentuid":
                    {
                        var rowId = rowFields.FirstOrDefault(o => o.Key == $"{proc.Get<string>("schemaAlias")}.{UIDMarker}");
                        return rowId.Value;
                    }
                case "documentid":
                    {
                        var rowId = rowFields.FirstOrDefault(o => o.Key == $"{proc.Get<string>("schemaAlias")}.{UIDMarker}");
                        if (rowId.Value == null)
                        {
                            return null;
                        }
                        return DocumentPointer.Parse(rowId.Value).DocumentId.ToString();
                    }
                case "documentpage":
                    {
                        var rowId = rowFields.FirstOrDefault(o => o.Key == $"{proc.Get<string>("schemaAlias")}.{UIDMarker}");
                        if (rowId.Value == null)
                        {
                            return null;
                        }
                        return DocumentPointer.Parse(rowId.Value).PageNumber.ToString();
                    }

                case "isgreater":
                    return (Condition.IsMatchGreater(transaction, proc.Get<int>("value1"), proc.Get<int>("value2")) == true).ToString();
                case "isless":
                    return (Condition.IsMatchLesser(transaction, proc.Get<int>("value1"), proc.Get<int>("value2")) == true).ToString();
                case "isgreaterorequal":
                    return (Condition.IsMatchGreater(transaction, proc.Get<int>("value1"), proc.Get<int>("value2")) == true).ToString();
                case "islessorequal":
                    return (Condition.IsMatchLesserOrEqual(transaction, proc.Get<int>("value1"), proc.Get<int>("value2")) == true).ToString();
                case "isbetween":
                    return (Condition.IsMatchBetween(transaction, proc.Get<int>("value"), proc.Get<int>("rangeLow"), proc.Get<int>("rangeHigh")) == true).ToString();
                case "isnotbetween":
                    return (Condition.IsMatchBetween(transaction, proc.Get<int>("value"), proc.Get<int>("rangeLow"), proc.Get<int>("rangeHigh")) == false).ToString();
                case "isequal":
                    return (Condition.IsMatchEqual(transaction, proc.Get<string>("text1"), proc.Get<string>("text2")) == true).ToString();
                case "isnotequal":
                    return (Condition.IsMatchEqual(transaction, proc.Get<string>("text1"), proc.Get<string>("text2")) == false).ToString();
                case "islike":
                    return (Condition.IsMatchLike(transaction, proc.Get<string>("text"), proc.Get<string>("pattern")) == true).ToString();
                case "isnotlike":
                    return (Condition.IsMatchLike(transaction, proc.Get<string>("text"), proc.Get<string>("pattern")) == false).ToString();

                case "guid":
                    return Guid.NewGuid().ToString();

                case "datetimeutc":
                    return DateTime.UtcNow.ToString(proc.Get<string>("format"));
                case "datetime":
                    return DateTime.Now.ToString(proc.Get<string>("format"));

                case "checksum":
                    return Library.Helpers.Checksum(proc.Get<string>("text")).ToString();
                case "sha1":
                    return Library.Helpers.GetSHA1Hash(proc.Get<string>("text"));
                case "sha256":
                    return Library.Helpers.GetSHA256Hash(proc.Get<string>("text"));
                case "indexof":
                    return proc.Get<string>("textToSearch").IndexOf(proc.Get<string>("textToFind")).ToString();
                case "lastindexof":
                    return proc.Get<string>("textToSearch").LastIndexOf(proc.Get<string>("textToFind")).ToString();
                case "right":
                    return proc.Get<string>("text").Substring(proc.Get<string>("text").Length - proc.Get<int>("length"));
                case "left":
                    return proc.Get<string>("text").Substring(0, proc.Get<int>("length"));
                case "iif":
                    {
                        if (proc.Get<bool>("condition"))
                            return proc.Get<string>("whenTrue");
                        else return proc.Get<string>("whenFalse");
                    }
                case "toproper":
                    return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(proc.Get<string>("text"));
                case "tolower":
                    return proc.Get<string>("text").ToLowerInvariant();
                case "toupper":
                    return proc.Get<string>("text").ToUpperInvariant();
                case "length":
                    return proc.Get<string>("text").Length.ToString();
                case "trim":
                    return proc.Get<string>("text").Trim();
                case "substring":
                    return proc.Get<string>("text").Substring(proc.Get<int>("startIndex"), proc.Get<int>("length"));
                case "concat":
                    {
                        var builder = new StringBuilder();
                        foreach (var p in parameters)
                        {
                            builder.Append(p);
                        }
                        return builder.ToString();
                    }
                case "coalesce":
                    {
                        foreach (var p in parameters)
                        {
                            if (p != null)
                            {
                                return p;
                            }
                        }
                        return null;
                    }

            }

            throw new KbFunctionException($"Undefined function: {functionName}.");
        }
    }
}
