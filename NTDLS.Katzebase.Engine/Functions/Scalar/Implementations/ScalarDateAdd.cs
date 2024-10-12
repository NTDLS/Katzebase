using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarDateAdd
    {
        //"Numeric DateAdd (String dateTime, String interval, Numeric offset)|'Increments the supplied date-time by the given interval and offset.'",
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var dateTimeString = function.Get<string?>("dateTime");
            var interval = function.Get<string?>("interval");
            var offset = function.Get<int?>("offset");

            if (dateTimeString == null || interval == null)
            {
                return null;
            }

            if (DateTime.TryParse(dateTimeString, out var dateTime) == false)
            {
                throw new KbProcessingException($"Expected a valid date-time expression, found: [{dateTimeString}].");
            }


            switch (interval)
            {
                case "dayofyear":
                case "year":
                case "yy":
                case "yyyy":
                    return dateTime.AddYears(offset.EnsureNotNull()).ToString();

                case "quarter":
                case "qq":
                case "q":
                    return dateTime.AddMonths(offset.EnsureNotNull() * 3).ToString();

                case "month":
                case "mm":
                case "m":
                    return dateTime.AddMonths(offset.EnsureNotNull()).ToString();

                case "weekday":
                case "dw":
                    {
                        int weekdaysToAdd = offset.EnsureNotNull();

                        int direction = weekdaysToAdd < 0 ? -1 : 1;
                        weekdaysToAdd = Math.Abs(weekdaysToAdd);

                        while (weekdaysToAdd > 0)
                        {
                            dateTime = dateTime.AddDays(direction);

                            // If the new date is a weekday (Monday to Friday), reduce the remaining weekdays to add.
                            if (dateTime.DayOfWeek != DayOfWeek.Saturday && dateTime.DayOfWeek != DayOfWeek.Sunday)
                            {
                                weekdaysToAdd--;
                            }
                        }

                        return dateTime.ToString();
                    }

                case "week":
                case "ww":
                case "wk":
                    return dateTime.AddDays(offset.EnsureNotNull() * 7).ToString();

                case "day":
                case "dy":
                    return dateTime.AddDays(offset.EnsureNotNull()).ToString();

                case "hour":
                case "hh":
                    return dateTime.AddHours(offset.EnsureNotNull()).ToString();

                case "minute":
                case "mi":
                    return dateTime.AddMinutes(offset.EnsureNotNull()).ToString();

                case "second":
                case "s":
                    return dateTime.AddSeconds(offset.EnsureNotNull()).ToString();

                case "millisecond":
                case "ms":
                    return dateTime.AddMilliseconds(offset.EnsureNotNull()).ToString();

                default:
                    throw new KbProcessingException($"Expected a date-time offset interval, found: [{interval}].");
            }
        }
    }
}
