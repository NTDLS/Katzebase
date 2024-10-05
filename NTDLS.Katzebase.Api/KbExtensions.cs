using NTDLS.Katzebase.Api.Types;

namespace NTDLS.Katzebase.Api
{
    public static class KbExtensions
    {
        /// <summary>
        /// Maps the results in the result-set to a class object.
        /// </summary>
        public static IEnumerable<T> MapTo<T>(this Payloads.KbQueryDocumentListResult result) where T : new()
        {
            var results = new List<T>();
            var properties = KbReflectionCache.GetProperties(typeof(T));

            foreach (var row in result.Rows)
            {
                var obj = new T();
                for (int fieldIndex = 0; fieldIndex < result.Fields.Count; fieldIndex++)
                {
                    if (properties.TryGetValue(result.Fields[fieldIndex].Name, out var property) && fieldIndex < row.Values.Count)
                    {
                        var value = row.Values[fieldIndex];

                        if (value == null)
                        {
                            try
                            {
                                if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                                {
                                    continue; // Skip setting value if property is non-nullable value type
                                }
                                else
                                {
                                    property.SetValue(obj, null);
                                }
                            }
                            catch
                            {
                                throw new Exception($"Failed to convert field [{result.Fields[fieldIndex].Name}] value [{value}] to type [{property.PropertyType.Name}].");
                            }
                        }
                        else
                        {
                            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                            try
                            {
                                property.SetValue(obj, Convert.ChangeType(value, propertyType));
                            }
                            catch
                            {
                                throw new Exception($"Failed to convert field [{result.Fields[fieldIndex].Name}] value [{value}] to type [{propertyType.Name}].");
                            }
                        }
                    }
                }
                results.Add(obj);
            }

            return results;
        }

        /// <summary>
        /// Converts an anonymous class object to a collection of parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Dictionary<string, KbConstant>? ToUserParametersDictionary(this object? parameters)
        {
            Dictionary<string, KbConstant>? result = null;
            if (parameters != null)
            {
                result = new();
                var type = parameters.GetType();

                foreach (var prop in type.GetProperties())
                {
                    var rawValue = prop.GetValue(parameters);
                    if (rawValue is string)
                    {
                        result.Add('@' + prop.Name, new KbConstant(rawValue?.ToString(), KbConstants.KbBasicDataType.String));
                    }
                    else
                    {
                        if (rawValue == null || double.TryParse(rawValue?.ToString(), out _))
                        {
                            result.Add('@' + prop.Name, new KbConstant(rawValue?.ToString(), KbConstants.KbBasicDataType.Numeric));
                        }
                        else
                        {
                            throw new Exception($"Non-string value of [{prop.Name}] cannot be converted to numeric.");
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Converts an anonymous class object to a collection of parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static KbInsensitiveDictionary<KbConstant>? ToUserParametersInsensitiveDictionary(this object? parameters)
        {
            KbInsensitiveDictionary<KbConstant>? result = null;
            if (parameters != null)
            {
                result = new();
                var type = parameters.GetType();

                foreach (var prop in type.GetProperties())
                {
                    var rawValue = prop.GetValue(parameters);
                    if (rawValue is string)
                    {
                        result.Add('@' + prop.Name, new KbConstant(rawValue?.ToString(), KbConstants.KbBasicDataType.String));
                    }
                    else
                    {
                        if (rawValue == null || double.TryParse(rawValue?.ToString(), out _))
                        {
                            result.Add('@' + prop.Name, new KbConstant(rawValue?.ToString(), KbConstants.KbBasicDataType.Numeric));
                        }
                        else
                        {
                            throw new Exception($"Non-string value of [{prop.Name}] cannot be converted to numeric.");
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Converts an collection of Key-Value-Pairs to a collection of parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static KbInsensitiveDictionary<KbConstant>? ToUserParametersInsensitiveDictionary(this Dictionary<string, object?> parameters)
        {
            if (parameters == null)
            {
                return null;
            }

            var result = new KbInsensitiveDictionary<KbConstant>();
            foreach (var parameter in parameters)
            {
                if (parameter.Value is string)
                {
                    result.Add('@' + parameter.Key, new KbConstant(parameter.Value?.ToString(), KbConstants.KbBasicDataType.String));
                }
                else
                {
                    if (parameter.Value == null || double.TryParse(parameter.Value?.ToString(), out _))
                    {
                        result.Add('@' + parameter.Key, new KbConstant(parameter.Value?.ToString(), KbConstants.KbBasicDataType.Numeric));
                    }
                    else
                    {
                        throw new Exception($"Non-string value of [{parameter.Key}] cannot be converted to numeric.");
                    }
                }
            }
            return result;
        }
    }
}
