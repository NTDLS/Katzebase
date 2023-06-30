using Katzebase.Engine.Documents;
using Katzebase.Engine.Library;
using Katzebase.Engine.Query.FunctionParameter;
using Katzebase.PublicLibrary.Exceptions;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text;

namespace Katzebase.Engine.Query.Function.Scaler
{
    /// <summary>
    /// Contains all function protype defintions, function implemtations and expression collapse functionality.
    /// </summary>
    internal class QueryScalerFunctionImplementation
    {
        internal static string[] FunctionPrototypes = {
                "Guid:",
                "Equals:string/text1,string/text2",
                "DocumentUID:string/schemaAlias",
                "DocumentPage:string/schemaAlias",
                "DocumentID:string/schemaAlias",
                "DateTimeUTC:optional_string/format=yyyy-MM-dd HH:mm:ss.ms",
                "DateTime:optional_string/format=yyyy-MM-dd HH:mm:ss.ms",
                "ToProper:string/text",
                "ToLower:string/text",
                "ToUpper:string/text",
                "Length:string/text",
                "SubString:string/text,integer/startIndex,integer/length",
                "ConCat:infinite_string/text",
                "Trim:string/text",
                "Checksum:string/text",
                "Sha1:string/text",
                "IndexOf:string/textToFind,string/textToSearch",
                "LastIndexOf:string/textToFind,string/textToSearch",
                "Sha256:string/text",
                "Right:string/text,integer/length",
                "Left:string/text,integer/length",
                "IIF:boolean/condition,string/whenTrue,string/whenFalse",
            };

        internal static string? CollapseAllFunctionParameters(FunctionParameterBase param, Dictionary<string, string?> rowFields)
        {
            if (param is FunctionConstantParameter)
            {
                return ((FunctionConstantParameter)param).Value;
            }
            else if (param is FunctionDocumentFieldParameter)
            {
                var result = rowFields.Where(o => o.Key == ((FunctionDocumentFieldParameter)param).Value.Key).SingleOrDefault().Value;

                if (result == null)
                {
                    throw new KbMethodException($"Field was not found when processing method: {((FunctionDocumentFieldParameter)param).Value.Key}.");
                }

                return result;
            }
            else if (param is FunctionExpression)
            {
                var expression = new NCalc.Expression(((FunctionExpression)param).Value.Replace("{", "(").Replace("}", ")"));

                foreach (var subParam in ((FunctionExpression)param).Parameters)
                {
                    if (subParam is FunctionMethodAndParams)
                    {
                        string variable = ((FunctionNamedMethodAndParams)subParam).ExpressionKey.Replace("{", "").Replace("}", "");
                        var value = CollapseAllFunctionParameters(subParam, rowFields);
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
            else if (param is FunctionMethodAndParams)
            {
                var subParams = new List<string?>();

                foreach (var subParam in ((FunctionMethodAndParams)param).Parameters)
                {
                    subParams.Add(CollapseAllFunctionParameters(subParam, rowFields));
                }

                return ExecuteMethod(((FunctionMethodAndParams)param).Method, subParams, rowFields);
            }
            else
            {
                //What is this?
                throw new KbNotImplementedException();
            }
        }


        private static string? ExecuteMethod(string methodName, List<string?> parameters, Dictionary<string, string?> rowFields)
        {
            var method = QueryScalerFunctionCollection.ApplyMethodPrototype(methodName, parameters);

            switch (methodName.ToLower())
            {
                case "documentuid":
                    {
                        var rowId = rowFields.Where(o => o.Key == $"{method.Get<string>("schemaAlias")}.$UID$").FirstOrDefault();
                        return rowId.Value;
                    }
                case "documentid":
                    {
                        var rowId = rowFields.Where(o => o.Key == $"{method.Get<string>("schemaAlias")}.$UID$").FirstOrDefault();
                        if (rowId.Value == null)
                        {
                            return null;
                        }
                        return DocumentPointer.Parse(rowId.Value).DocumentId.ToString();
                    }
                case "documentpage":
                    {
                        var rowId = rowFields.Where(o => o.Key == $"{method.Get<string>("schemaAlias")}.$UID$").FirstOrDefault();
                        if (rowId.Value == null)
                        {
                            return null;
                        }
                        return DocumentPointer.Parse(rowId.Value).PageNumber.ToString();
                    }

                case "equals":
                    return (method.Get<string>("text1") == method.Get<string>("text2")).ToString();

                case "guid":
                    return Guid.NewGuid().ToString();

                case "datetimeutc":
                    return DateTime.UtcNow.ToString(method.Get<string>("format"));
                case "datetime":
                    return DateTime.Now.ToString(method.Get<string>("format"));

                case "checksum":
                    return Helpers.Checksum(method.Get<string>("text")).ToString();
                case "sha1":
                    return Helpers.GetSHA1Hash(method.Get<string>("text")).ToString();
                case "sha256":
                    return Helpers.GetSHA256Hash(method.Get<string>("text")).ToString();
                case "indexof":
                    return method.Get<string>("textToSearch").IndexOf(method.Get<string>("textToFind")).ToString();
                case "lastindexof":
                    return method.Get<string>("textToSearch").LastIndexOf(method.Get<string>("textToFind")).ToString();
                case "right":
                    return method.Get<string>("text").Substring(method.Get<string>("text").Length - method.Get<int>("length"));
                case "left":
                    return method.Get<string>("text").Substring(0, method.Get<int>("length"));
                case "iif":
                    {
                        if (method.Get<bool>("condition"))
                            return method.Get<string>("whenTrue");
                        else return method.Get<string>("whenFalse");
                    }
                case "toproper":
                    return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(method.Get<string>("text"));
                case "tolower":
                    return method.Get<string>("text").ToLowerInvariant();
                case "toupper":
                    return method.Get<string>("text").ToUpperInvariant();
                case "length":
                    return method.Get<string>("text").Length.ToString();
                case "trim":
                    return method.Get<string>("text").Trim();
                case "substring":
                    return method.Get<string>("text").Substring(method.Get<int>("startIndex"), method.Get<int>("length"));
                case "concat":
                    {
                        var builder = new StringBuilder();
                        foreach (var p in parameters)
                        {
                            builder.Append(p);
                        }
                        return builder.ToString();
                    }
            }

            throw new KbMethodException($"Undefined method: {methodName}.");
        }
    }
}
