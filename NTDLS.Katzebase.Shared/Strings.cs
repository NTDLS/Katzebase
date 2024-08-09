using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NTDLS.Katzebase.Shared
{
    public static class Strings
    {
        private static readonly MemoryCache _cache = MemoryCache.Default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(this string value, string otherValue)
        {
            return string.Equals(value, otherValue, StringComparison.InvariantCultureIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOneOf(this string value, string[] otherValues)
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

            var regex = (Regex?)_cache.Get(cacheKey);
            if (regex == null)
            {
                regex = new Regex("^" + Regex.Escape(pattern).Replace("%", ".*").Replace("_", ".") + "$",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);

                var cacheItemPolicy = new CacheItemPolicy
                {
                    SlidingExpiration = TimeSpan.FromMinutes(1)
                };

                _cache.Add(cacheKey, regex, cacheItemPolicy);
            }

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
