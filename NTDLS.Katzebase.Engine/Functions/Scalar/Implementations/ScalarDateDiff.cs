using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Functions.Scalar;

namespace NTDLS.Katzebase.Engine.Functions.Scalar.Implementations
{
    internal static class ScalarDateDiff
    {
        public static string? Execute(ScalarFunctionParameterValueCollection function)
        {
            var date1String = function.Get<string>("date1");
            var date2String = function.Get<string>("date2");
            var interval = function.Get<string>("interval").ToLowerInvariant();

            if (DateTime.TryParse(date1String, out var date1) == false)
            {
                throw new KbProcessingException($"Expected a valid date-time expression, found: [{date1String}].");
            }

            if (DateTime.TryParse(date2String, out var date2) == false)
            {
                throw new KbProcessingException($"Expected a valid date-time expression, found: [{date2String}].");
            }

            var timespan = date2 - date1;

            switch (interval)
            {
                case "dayofyear":
                    {
                        // Get the day of the year for each date
                        int startDayOfYear = date1.DayOfYear;
                        int endDayOfYear = date2.DayOfYear;

                        // If the dates are in different years, account for the full years in between
                        int yearDifference = date2.Year - date1.Year;

                        // Calculate the difference in days
                        int dayDifference = 0;
                        if (yearDifference == 0)
                        {
                            // If the dates are in the same year, subtract directly
                            dayDifference = endDayOfYear - startDayOfYear;
                        }
                        else
                        {
                            // If the dates span multiple years, account for complete years
                            int startYearDays = DateTime.IsLeapYear(date1.Year) ? 366 : 365;
                            dayDifference = (endDayOfYear + (startYearDays - startDayOfYear)) + (yearDifference - 1) * 365;

                            // Add leap years for any full years in between
                            for (int i = date1.Year + 1; i < date2.Year; i++)
                            {
                                if (DateTime.IsLeapYear(i))
                                {
                                    dayDifference++;
                                }
                            }
                        }

                        return dayDifference.ToString();
                    }

                case "year":
                case "yy":
                case "yyyy":
                    {
                        int years = date2.Year - date1.Year;
                        // If the end date is before the start date's month/day in the current year, subtract one year
                        if (date2 < date1.AddYears(years)) //Accounts for partial years.
                        {
                            years--;
                        }
                        return years.ToString();
                    }

                case "quarter":
                case "qq":
                case "q":
                    {
                        int startQuarter = (date1.Month - 1) / 3 + 1;
                        int endQuarter = (date2.Month - 1) / 3 + 1;

                        int yearDifference = date2.Year - date1.Year;
                        int quarterDifference = yearDifference * 4 + (endQuarter - startQuarter);

                        // If the end day is earlier in the quarter than the start day, subtract one quarter
                        if (date2 < date1.AddMonths((quarterDifference) * 3))
                        {
                            quarterDifference--;
                        }

                        return quarterDifference.ToString();
                    }

                case "month":
                case "mm":
                case "m":
                    {
                        int months = (date2.Year - date1.Year) * 12 + date2.Month - date1.Month;

                        // If the end day is earlier in the month than the start day, subtract one month
                        if (date2.Day < date1.Day)
                        {
                            months--;
                        }

                        return months.ToString();
                    }

                case "weekday":
                case "dw":
                    {
                        if (date1 > date2)
                        {
                            // Swap if start is after end to always go forward in time
                            DateTime temp = date1;
                            date1 = date2;
                            date2 = temp;
                        }

                        int weekdayCount = 0;

                        // Loop through each day from start to end
                        for (DateTime currentDate = date1; currentDate <= date2; currentDate = currentDate.AddDays(1))
                        {
                            // Check if the current day is not Saturday (6) or Sunday (0)
                            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                            {
                                weekdayCount++;
                            }
                        }

                        return weekdayCount.ToString();
                    }

                case "week":
                case "ww":
                case "wk":
                    return ((date2 - date1).TotalDays / 7).ToString();

                case "day":
                case "dy":
                        return (date2 - date1).TotalDays.ToString();

                case "hour":
                case "hh":
                    return (date2 - date1).TotalHours.ToString();

                case "minute":
                case "mi":
                    return (date2 - date1).TotalMinutes.ToString();

                case "second":
                case "s":
                    return (date2 - date1).TotalSeconds.ToString();

                case "millisecond":
                case "ms":
                    return (date2 - date1).TotalMilliseconds.ToString();

                default:
                    throw new KbProcessingException($"Expected a date-time difference interval, found: [{interval}].");
            }
        }
    }
}
