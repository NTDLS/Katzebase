using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;

namespace NTDLS.Katzebase.Api
{
    public static class KbExtensions
    {
        /// <summary>
        /// Maps the rows of a result to objects. Fields are matched to writable public properties by name (case-insensitive),
        /// fields without a matching property are ignored, and values are converted with <see cref="KbValueConverter"/>.
        /// </summary>
        public static List<T> MapTo<T>(this Payloads.Response.KbQueryResult result) where T : new()
        {
            var results = new List<T>(result.Rows.Count);
            var properties = KbReflectionCache.GetProperties(typeof(T));

            //Resolve the property for each field once, rather than once per row.
            var fieldProperties = result.Fields
                .Select(field => properties.TryGetValue(field.Name, out var property) ? property : null)
                .ToArray();

            foreach (var row in result.Rows)
            {
                var obj = new T();
                for (int fieldIndex = 0; fieldIndex < fieldProperties.Length && fieldIndex < row.Values.Count; fieldIndex++)
                {
                    var property = fieldProperties[fieldIndex];
                    if (property == null)
                    {
                        continue;
                    }

                    var value = row.Values[fieldIndex];
                    if (value == null && property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                    {
                        continue; //Leave non-nullable value types at their default.
                    }

                    try
                    {
                        property.SetValue(obj, KbValueConverter.Convert(value, property.PropertyType));
                    }
                    catch (Exception ex)
                    {
                        throw new KbProcessingException($"Failed to map field [{result.Fields[fieldIndex].Name}] value [{value}] to [{typeof(T).Name}.{property.Name}]: {ex.Message}");
                    }
                }
                results.Add(obj);
            }

            return results;
        }

        /// <summary>
        /// Converts query parameters (an anonymous object, any object, or a dictionary of names to values) to the variables
        /// that are sent to the server. See <see cref="KbParameters.Convert(object?)"/>.
        /// </summary>
        public static KbInsensitiveDictionary<KbVariable>? ToUserParametersInsensitiveDictionary(this object? parameters)
            => KbParameters.Convert(parameters);
    }
}
