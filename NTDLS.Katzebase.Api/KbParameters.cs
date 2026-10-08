using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using System.Collections;
using System.Globalization;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Api
{
    /// <summary>
    /// Converts query parameters into the variables sent to the server.
    ///
    /// Parameters can be given as:
    ///   - an anonymous object or any other object, whose public properties are the parameters: new { Id = 1, Name = "x" }
    ///   - a dictionary of names to values: Dictionary&lt;string, object?&gt;, IDictionary, IReadOnlyDictionary&lt;string, object?&gt;
    ///   - an already converted KbInsensitiveDictionary&lt;KbVariable&gt;
    /// Names may be given with or without the leading '@'. Values are formatted with the invariant culture so that queries
    /// behave the same on every machine (e.g. 1.5 is always sent as "1.5", never "1,5").
    /// </summary>
    public static class KbParameters
    {
        public static KbInsensitiveDictionary<KbVariable>? Convert(object? parameters)
        {
            switch (parameters)
            {
                case null:
                    return null;
                case KbInsensitiveDictionary<KbVariable> variables:
                    return variables;
                case TimeSpan:
                    //Almost certainly a query timeout passed in the parameters position.
                    throw new KbInvalidArgumentException("A TimeSpan was given as query parameters. Pass query timeouts using the timeout argument.");
                case string:
                    throw new KbInvalidArgumentException("A string was given as query parameters. Pass parameters as an object or dictionary of names and values.");
            }

            var result = new KbInsensitiveDictionary<KbVariable>();

            if (parameters is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                foreach (var pair in pairs)
                {
                    Add(result, pair.Key, pair.Value);
                }
            }
            else if (parameters is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    Add(result, entry.Key?.ToString() ?? throw new KbInvalidArgumentException("Parameter names cannot be null."), entry.Value);
                }
            }
            else
            {
                foreach (var property in parameters.GetType().GetProperties())
                {
                    if (property.CanRead && property.GetIndexParameters().Length == 0)
                    {
                        Add(result, property.Name, property.GetValue(parameters));
                    }
                }
            }

            return result;
        }

        private static void Add(KbInsensitiveDictionary<KbVariable> result, string name, object? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new KbInvalidArgumentException("Parameter names cannot be empty.");
            }

            var key = name.StartsWith('@') ? name : '@' + name;
            if (result.ContainsKey(key))
            {
                throw new KbInvalidArgumentException($"Parameter [{key}] was given more than once.");
            }

            result.Add(key, ToVariable(value));
        }

        /// <summary>
        /// Converts a value to a query variable, formatting it with the invariant culture.
        /// </summary>
        public static KbVariable ToVariable(object? value)
        {
            return value switch
            {
                null => new KbVariable(null, KbBasicDataType.String),
                KbVariable variable => variable,
                string text => new KbVariable(text, KbBasicDataType.String),
                bool boolean => new KbVariable(boolean ? "1" : "0", KbBasicDataType.Numeric),
                byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal
                    => new KbVariable(System.Convert.ToString(value, CultureInfo.InvariantCulture), KbBasicDataType.Numeric),
                Enum enumValue => new KbVariable(System.Convert.ToInt64(enumValue, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture), KbBasicDataType.Numeric),
                DateTime dateTime => new KbVariable(dateTime.ToString("o", CultureInfo.InvariantCulture), KbBasicDataType.String),
                DateTimeOffset dateTimeOffset => new KbVariable(dateTimeOffset.ToString("o", CultureInfo.InvariantCulture), KbBasicDataType.String),
                DateOnly date => new KbVariable(date.ToString("o", CultureInfo.InvariantCulture), KbBasicDataType.String),
                TimeOnly time => new KbVariable(time.ToString("o", CultureInfo.InvariantCulture), KbBasicDataType.String),
                IFormattable formattable => new KbVariable(formattable.ToString(null, CultureInfo.InvariantCulture), KbBasicDataType.String),
                _ => new KbVariable(value.ToString(), KbBasicDataType.String)
            };
        }
    }
}
