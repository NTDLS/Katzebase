using Katzebase.Engine.Documents;
using Katzebase.Engine.Library;
using Katzebase.Engine.Method.ParsedMethodParameter;
using Katzebase.PublicLibrary.Exceptions;
using System.Globalization;
using System.Text;

namespace Katzebase.Engine.Method
{
    internal class MethodImplementations
    {
        static string[] FunctionPrototypes = {
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

        #region Parsing and Validation.

        internal static string CollapseAllFunctionParameters(GenericParsedMethodParameter param, Dictionary<string, string> rowFields)
        {
            if (param is ParsedConstantParameter)
            {
                return ((ParsedConstantParameter)param).Value;
            }
            else if (param is ParsedFieldParameter)
            {
                var result = rowFields.Where(o => o.Key == ((ParsedFieldParameter)param).Value.Key).SingleOrDefault().Value;

                if (result == null)
                {
                    throw new KbMethodException($"Field was not found when processing method: {((ParsedFieldParameter)param).Value.Key}.");
                }

                return result;
            }
            else if (param is ParsedExpression)
            {
                var expression = new NCalc.Expression(((ParsedExpression)param).Value.Replace("{", "(").Replace("}", ")"));

                foreach (var subParam in ((ParsedExpression)param).Parameters)
                {
                    if (subParam is ParsedMethodAndParams)
                    {
                        string variable = ((ParsedNamedMethodAndParams)subParam).ExpressionKey.Replace("{", "").Replace("}", "");
                        decimal value = decimal.Parse(CollapseAllFunctionParameters(subParam, rowFields));
                        expression.Parameters.Add(variable, value);
                    }
                    else if (subParam is ParsedFieldParameter)
                    {
                        string variable = ((ParsedFieldParameter)subParam).ExpressionKey.Replace("{", "").Replace("}", "");
                        var value = decimal.Parse(rowFields.Where(o => o.Key == ((ParsedFieldParameter)subParam).Value.Key).SingleOrDefault().Value);
                        expression.Parameters.Add(variable, value);
                    }
                    else
                    {
                        throw new KbNotImplementedException();
                    }
                }

                return expression.Evaluate()?.ToString() ?? string.Empty;
            }
            else if (param is ParsedMethodAndParams)
            {
                var subParams = new List<string>();

                foreach (var subParam in ((ParsedMethodAndParams)param).Parameters)
                {
                    subParams.Add(CollapseAllFunctionParameters(subParam, rowFields));
                }

                return ExecuteMethod(((ParsedMethodAndParams)param).Method, subParams, rowFields);
            }
            else
            {
                //What is this?
                throw new KbNotImplementedException();
            }
        }

        internal enum KbParameterType
        {
            Undefined,
            String,
            Boolean,
            Integer,
            Infinite_String,
            optional_string
        }

        internal class KbFunctionParameter
        {
            public KbParameterType Type { get; set; }
            public string Name { get; set; }
            public string? DefaultValue { get; set; }

            public KbFunctionParameter(KbParameterType type, string name)
            {
                Type = type;
                Name = name;
            }

            public KbFunctionParameter(KbParameterType type, string name, string defaultValue)
            {
                Type = type;
                Name = name;
                DefaultValue = defaultValue;
            }
        }

        internal class KbFunctionParameterValues
        {
            public List<KbFunctionParameterValue> Values { get; set; } = new();

            public T Get<T>(string name)
            {
                try
                {
                    var parameter = Values.Where(o => o.Parameter.Name.ToLower() == name.ToLower()).FirstOrDefault();
                    if (parameter == null)
                    {
                        throw new KbGenericException($"Value for {name} cannot be null.");
                    }

                    if (parameter.Value == null)
                    {
                        if (parameter.Parameter.DefaultValue == null)
                        {
                            throw new KbGenericException($"Value for {name} cannot be null.");
                        }
                        return Helpers.ConvertTo<T>(parameter.Parameter.DefaultValue);
                    }

                    return Helpers.ConvertTo<T>(parameter.Value);
                }
                catch
                {
                    throw new KbGenericException($"Undefined parameter {name}.");
                }
            }

            public T Get<T>(string name, T defaultValue)
            {
                try
                {
                    var value = Values.Where(o => o.Parameter.Name.ToLower() == name.ToLower()).FirstOrDefault()?.Value;
                    if (value == null)
                    {
                        return defaultValue;
                    }

                    return Helpers.ConvertTo<T>(value);
                }
                catch
                {
                    throw new KbGenericException($"Undefined parameter {name}.");
                }
            }
        }

        internal class KbFunctionParameterValue
        {
            public KbFunctionParameter Parameter { get; set; }
            public string? Value { get; set; } = null;

            public KbFunctionParameterValue(KbFunctionParameter parameter, string value)
            {
                Parameter = parameter;
                Value = value;
            }

            public KbFunctionParameterValue(KbFunctionParameter parameter)
            {
                Parameter = parameter;
            }
        }

        internal class KbFunction
        {
            public string Name { get; set; }
            public List<KbFunctionParameter> Parameters { get; private set; } = new();

            public KbFunction(string name, List<KbFunctionParameter> parameters)
            {
                Name = name;
                Parameters.AddRange(parameters);
            }

            public static KbFunction Parse(string prototype)
            {
                int indexOfMethodNameEnd = prototype.IndexOf(':');
                string methodName = prototype.Substring(0, indexOfMethodNameEnd);
                var parameterStrings = prototype.Substring(indexOfMethodNameEnd + 1).Split(',',  StringSplitOptions.RemoveEmptyEntries);
                List<KbFunctionParameter> parameters = new();

                foreach (var param in parameterStrings)
                {
                    var typeAndName = param.Split("/");
                    if (Enum.TryParse(typeAndName[0], true, out KbParameterType paramType) == false)
                    {
                        throw new KbGenericException($"Unknown parameter type {typeAndName[0]}");
                    }

                    var nameAndDefault = typeAndName[1].Trim().Split('=');

                    if (nameAndDefault.Count() == 1)
                    {
                        parameters.Add(new KbFunctionParameter(paramType, nameAndDefault[0]));
                    }
                    else if (nameAndDefault.Count() == 2)
                    {
                        parameters.Add(new KbFunctionParameter(paramType, nameAndDefault[0], nameAndDefault[1]));
                    }
                    else
                    {
                        throw new KbGenericException($"Wrong number of default parameters supplied to prototype for {typeAndName[0]}");
                    }
                }

                return new KbFunction(methodName, parameters);
            }

            internal KbFunctionParameterValues ApplyParameters(List<string> values)
            {
                int requiredParameterCount = Parameters.Where(o => o.Type.ToString().ToLower().Contains("optional") == false).Count();

                if (Parameters.Count < requiredParameterCount)
                {
                    if (Parameters.Count > 0 && Parameters[0].Type == KbParameterType.Infinite_String)
                    {
                        //The first parameter is infinite, we dont even check anything else.
                    }
                    else
                    {
                        throw new KbMethodException($"Incorrect number of parameter passed to {Name}.");
                    }
                }

                var result = new KbFunctionParameterValues();

                if (Parameters.Count > 0 && Parameters[0].Type == KbParameterType.Infinite_String)
                {
                    for (int i = 0; i < Parameters.Count; i++)
                    {
                        result.Values.Add(new KbFunctionParameterValue(Parameters[0], values[i]));
                    }
                }
                else
                {
                    for (int i = 0; i < Parameters.Count; i++)
                    {
                        if (i >= values.Count)
                        {
                            result.Values.Add(new KbFunctionParameterValue(Parameters[i]));
                        }
                        else
                        {
                            result.Values.Add(new KbFunctionParameterValue(Parameters[i], values[i]));
                        }
                    }
                }

                return result;
            }
        }

        internal static class KbFunctions
        {
            private static List<KbFunction>? _protypes = null;

            public static void Initialize()
            {
                if (_protypes == null)
                {
                    _protypes = new List<KbFunction>();

                    foreach (var prototype in FunctionPrototypes)
                    {
                        _protypes.Add(KbFunction.Parse(prototype));
                    }
                }
            }

            public static KbFunctionParameterValues ApplyMethodPrototype(string methodName, List<string> parameters)
            {
                if (_protypes == null)
                {
                    throw new KbFatalException("Method prototypes were not initialized.");
                }

                var method = _protypes.Where(o => o.Name.ToLower() == methodName.ToLower()).FirstOrDefault();

                if (method == null)
                {
                    throw new KbMethodException($"Undefined method: {methodName}.");
                }

                return method.ApplyParameters(parameters);
            }
        }

        #endregion

        private static string ExecuteMethod(string methodName, List<string> parameters, Dictionary<string, string> rowFields)
        {
            var method = KbFunctions.ApplyMethodPrototype(methodName, parameters);

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
                        return DocumentPointer.Parse(rowId.Value).DocumentId.ToString();
                    }
                case "documentpage":
                    {
                        var rowId = rowFields.Where(o => o.Key == $"{method.Get<string>("schemaAlias")}.$UID$").FirstOrDefault();
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
