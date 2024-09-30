using Microsoft.Extensions.Caching.Memory;
using NTDLS.Helpers;
using System.Reflection;

namespace NTDLS.Katzebase.Client
{
    class KbReflectionCache
    {
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

        public static Dictionary<string, PropertyInfo> GetProperties(Type type)
        {
            if (!_cache.TryGetValue(type, out Dictionary<string, PropertyInfo>? properties))
            {
                properties = type.GetProperties().ToDictionary(p => p.Name, StringComparer.InvariantCultureIgnoreCase);

                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                };

                _cache.Set(type, properties, cacheEntryOptions);
            }

            return properties.EnsureNotNull();
        }
    }
}
