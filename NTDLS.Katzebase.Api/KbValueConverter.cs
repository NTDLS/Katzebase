using NTDLS.Katzebase.Api.Exceptions;
using System.Globalization;

namespace NTDLS.Katzebase.Api
{
    /// <summary>
    /// Converts the text values returned by the server to .NET types, using the invariant culture so that results are read
    /// the same way on every machine.
    /// </summary>
    public static class KbValueConverter
    {
        /// <summary>
        /// Converts a value returned by the server to the given type.
        /// Null is returned as null for reference and nullable types, and as the default value for other value types.
        /// </summary>
        public static object? Convert(string? value, Type targetType)
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            var type = underlyingType ?? targetType;

            if (value == null)
            {
                return (type.IsValueType && underlyingType == null) ? Activator.CreateInstance(type) : null;
            }

            try
            {
                if (type == typeof(string) || type == typeof(object))
                {
                    return value;
                }
                if (type == typeof(bool))
                {
                    //The engine represents booleans as 1 and 0.
                    return value.Trim() switch
                    {
                        "1" => true,
                        "0" => false,
                        var text => bool.Parse(text)
                    };
                }
                if (type == typeof(Guid))
                {
                    return Guid.Parse(value);
                }
                if (type.IsEnum)
                {
                    return Enum.Parse(type, value, ignoreCase: true);
                }
                if (type == typeof(DateTime))
                {
                    return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                }
                if (type == typeof(DateTimeOffset))
                {
                    return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                }
                if (type == typeof(DateOnly))
                {
                    return DateOnly.Parse(value, CultureInfo.InvariantCulture);
                }
                if (type == typeof(TimeOnly))
                {
                    return TimeOnly.Parse(value, CultureInfo.InvariantCulture);
                }
                if (type == typeof(TimeSpan))
                {
                    return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
                }
                if (IsIntegerType(type) && decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                    && number == decimal.Truncate(number))
                {
                    //Integers may be returned in a decimal form by expressions (e.g. "3.0").
                    return System.Convert.ChangeType(number, type, CultureInfo.InvariantCulture);
                }

                return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new KbProcessingException($"Value [{value}] cannot be converted to type [{type.Name}]: {ex.Message}");
            }
        }

        public static T? Convert<T>(string? value)
            => (T?)Convert(value, typeof(T));

        private static bool IsIntegerType(Type type)
            => type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
    }
}
