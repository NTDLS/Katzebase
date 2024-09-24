using fs;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NTDLS.Katzebase.Shared
{
    public static class Strings
    {
        private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        private static readonly MemoryCacheEntryOptions _oneMinuteSlidingExpiration
            = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(this string value, string? otherValue)
        {
            return string.Equals(value, otherValue, StringComparison.InvariantCultureIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(this fstring value, fstring? otherValue)
        {
            return fstring.Compare(value, otherValue) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(this fstring value, string? otherValue)
        {
            return value.s == otherValue;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNot(this string value, string? otherValue)
        {
            return !string.Equals(value, otherValue, StringComparison.InvariantCultureIgnoreCase);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNot(this fstring value, fstring? otherValue)
        {
            return fstring.Compare(value, otherValue) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOneOf(this string value, string?[] otherValues)
        {
            foreach (var otherValue in otherValues)
            {
                if (string.Equals(value, otherValue, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsInsensitive(this string value, string otherValue)
        {
            return value.Contains(otherValue, StringComparison.InvariantCultureIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsInsensitive(this string value, string[] otherValues)
        {
            foreach (var otherValue in otherValues)
            {
                if (value.Contains(otherValue, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLike(this string value, string pattern)
        {
            string cacheKey = $"IsMatchLike:{pattern}";

            if (_cache.TryGetValue<Regex>(cacheKey, out var regex) == false)
            {
                regex = new Regex("^" + Regex.Escape(pattern).Replace("%", ".*").Replace("_", ".") + "$",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);

                _cache.Set(cacheKey, regex, _oneMinuteSlidingExpiration);
            }

            ArgumentNullException.ThrowIfNull(regex);

            return regex.IsMatch(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLikeOneOf(this string value, string[] otherValues)
        {
            foreach (var otherValue in otherValues)
            {
                if (value.Contains(otherValue, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
