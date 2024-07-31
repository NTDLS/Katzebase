using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Functions.Parameters;
using NTDLS.Katzebase.Engine.Query.Constraints;
using System.Globalization;
using System.Text;

namespace NTDLS.Katzebase.Engine.Functions.Scaler
{
    /// <summary>
    /// Contains all function protype defintions, function implementations and expression collapse functionality.
    /// </summary>
    internal class ScalerFunctionImplementation
    {
        internal static string[] PrototypeStrings = {
                //"Function_Name:ReturnType:param_type/Param_Name=default_value,param_type/Param_Name=default_value
                "Guid:string:",
                "IsGreater:boolean:numeric/value1,numeric/value2",
                "IsLess:boolean:numeric/value1,numeric/value2",
                "IsGreaterOrEqual:boolean:numeric/value1,numeric/value2",
                "IsLessOrEqual:boolean:numeric/value1,numeric/value2",
                "IsBetween:boolean:numeric/value,numeric/rangeLow,numeric/rangeHigh",
                "IsNotBetween:boolean:numeric/value,numeric/rangeLow,numeric/rangeHigh",
                "IsEqual:boolean:string/text1,string/text2",
                "IsNotEqual:boolean:string/text1,string/text2",
                "IsLike:boolean:string/text,string/pattern",
                "IsNotLike:boolean:string/text,string/pattern",
                "DocumentUID:string:string/schemaAlias",
                "DocumentPage:string:string/schemaAlias",
                "DocumentID:string:string/schemaAlias",
                "DateTimeUTC:string:optional_string/format=yyyy-MM-dd HH:mm:ss.ms",
                "DateTime:string:optional_string/format=yyyy-MM-dd HH:mm:ss.ms",
                "ToProper:string:string/text",
                "ToLower:string:string/text",
                "ToUpper:string:string/text",
                "Length:numeric:string/text",
                "SubString:string:string/text,numeric/startIndex,numeric/length",
                "Coalesce:string:infinite_string/text",
                "Concat:string:infinite_string/text",
                "Trim:string:string/text",
                "Checksum:numeric:string/text",
                "Sha1:string:string/text",
                "IndexOf:string:string/textToFind,string/textToSearch",
                "LastIndexOf:numeric:string/textToFind,string/textToSearch",
                "Sha256:string:string/text",
                "Right:string:string/text,numeric/length",
                "Left:string:string/text,numeric/length",
                "IIF:string:boolean/condition,string/whenTrue,string/whenFalse",
            };

        internal static string? CollapseAllFunctionParameters(Transaction transaction, FunctionParameterBase param, KbInsensitiveDictionary<string?> rowFields)
        {
            if (param is FunctionConstantParameter)
            {
                var value = ((FunctionConstantParameter)param).RawValue;
                if (value.StartsWith('\'') && value.EndsWith('\''))
                {
                    return value.Substring(1, value.Length - 2);
                }
                return value;
            }
            else if (param is FunctionDocumentFieldParameter)
            {
                var result = rowFields.Where(o => o.Key == ((FunctionDocumentFieldParameter)param).Value.Key).SingleOrDefault().Value;

                if (result == null)
                {
                    throw new KbFunctionException($"Field was not found when processing function: {((FunctionDocumentFieldParameter)param).Value.Key}.");
                }

                return result;
            }
            else if (param is FunctionExpression)
            {
                var expression = new NCalc.Expression(((FunctionExpression)param).Value.Replace("{", "(").Replace("}", ")"));

                foreach (var subParam in ((FunctionExpression)param).Parameters)
                {
                    if (subParam is FunctionWithParams)
                    {
                        string variable = ((FunctionNamedWithParams)subParam).ExpressionKey.Replace("{", "").Replace("}", "");
                        var value = CollapseAllFunctionParameters(transaction, subParam, rowFields);
                        if (value != null)
                        {
                            expression.Parameters.Add(variable, decimal.Parse(value));
                        }
                        else
                        {
                            expression.Parameters.Add(variable, null);
                        }
                    }
                    else if (subParam is FunctionDocumentFieldParameter)
                    {
                        string variable = ((FunctionDocumentFieldParameter)subParam).ExpressionKey.Replace("{", "").Replace("}", "");
                        var value = rowFields.Where(o => o.Key == ((FunctionDocumentFieldParameter)subParam).Value.Key).SingleOrDefault().Value;
                        if (value != null)
                        {
                            expression.Parameters.Add(variable, decimal.Parse(value));
                        }
                        else
                        {
                            expression.Parameters.Add(variable, null);
                        }
                    }
                    else
                    {
                        throw new KbNotImplementedException();
                    }
                }

                return expression.Evaluate()?.ToString() ?? string.Empty;
            }
            else if (param is FunctionWithParams)
            {
                var subParams = new List<string?>();

                foreach (var subParam in ((FunctionWithParams)param).Parameters)
                {
                    subParams.Add(CollapseAllFunctionParameters(transaction, subParam, rowFields));
                }

                return ExecuteFunction(transaction, ((FunctionWithParams)param).Function, subParams, rowFields);
            }
            else
            {
                //What is this?
                throw new KbNotImplementedException();
            }
        }


        private static string? ExecuteFunction(Transaction transaction, string functionName, List<string?> parameters, KbInsensitiveDictionary<string?> rowFields)
        {
            var proc = ScalerFunctionCollection.ApplyFunctionPrototype(functionName, parameters);

            switch (functionName.ToLower())
            {
                case "documentuid":
                    {
                        var rowId = rowFields.Where(o => o.Key == $"{proc.Get<string>("schemaAlias")}.$UID$").FirstOrDefault();
                        return rowId.Value;
                    }
                case "documentid":
                    {
                        var rowId = rowFields.Where(o => o.Key == $"{proc.Get<string>("schemaAlias")}.$UID$").FirstOrDefault();
                        if (rowId.Value == null)
                        {
                            return null;
                        }
                        return DocumentPointer.Parse(rowId.Value).DocumentId.ToString();
                    }
                case "documentpage":
                    {
                        var rowId = rowFields.Where(o => o.Key == $"{proc.Get<string>("schemaAlias")}.$UID$").FirstOrDefault();
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
                    return Library.Helpers.GetSHA1Hash(proc.Get<string>("text")).ToString();
                case "sha256":
                    return Library.Helpers.GetSHA256Hash(proc.Get<string>("text")).ToString();
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
