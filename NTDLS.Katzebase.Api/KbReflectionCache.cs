using Microsoft.Extensions.Caching.Memory;
using NTDLS.Helpers;
using System.Reflection;

namespace NTDLS.Katzebase.Api
{
    class KbReflectionCache
    {
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

        public static Dictionary<string, PropertyInfo> GetProperties(Type type)
        {
            if (!_cache.TryGetValue(type, out Dictionary<string, PropertyInfo>? properties))
            {
                //Only writable, non-indexer properties can be mapped. If two properties differ only by case, the first wins.
                properties = new Dictionary<string, PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanWrite && property.GetSetMethod() != null && property.GetIndexParameters().Length == 0)
                    {
                        properties.TryAdd(property.Name, property);
                    }
                }

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
