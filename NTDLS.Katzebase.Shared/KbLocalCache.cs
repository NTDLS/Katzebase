using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Shared
{
    public static class KbLocalCache
    {
        private static readonly ConcurrentDictionary<object, SemaphoreSlim> _locks = new();
        internal static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        internal static readonly MemoryCacheEntryOptions _sliding2Second = new() { SlidingExpiration = TimeSpan.FromSeconds(2) };
        internal static readonly MemoryCacheEntryOptions _sliding1Minute = new() { SlidingExpiration = TimeSpan.FromMinutes(1) };
        internal static readonly MemoryCacheEntryOptions _sliding10Minutes = new() { SlidingExpiration = TimeSpan.FromMinutes(10) };
        internal static readonly MemoryCacheEntryOptions _sliding1Hour = new() { SlidingExpiration = TimeSpan.FromHours(1) };

        public static bool TryGet<T>(object key, [NotNullWhen(true)] out T? value)
            => _cache.TryGetValue(key, out value) && value != null;

        public static void SetSliding(object key, object value, TimeSpan slidingExpiration)
            => _cache.Set(key, value, new MemoryCacheEntryOptions() { SlidingExpiration = slidingExpiration });

        public static void Remove(object key)
            => _cache.Remove(key);

        public static TItem? GetOrCreateSliding10Seconds<TItem>(object key, Func<TItem?> factory)
            => GetOrCreate(key, () => factory(), _sliding2Second);

        public static TItem? GetOrCreateSliding1Minute<TItem>(object key, Func<TItem?> factory)
            => GetOrCreate(key, () => factory(), _sliding1Minute);

        public static TItem? GetOrCreateSliding10Minutes<TItem>(object key, Func<TItem> factory)
            => GetOrCreate(key, () => factory(), _sliding10Minutes);

        public static TItem? GetOrCreateSliding1Hour<TItem>(object key, Func<TItem> factory)
            => GetOrCreate(key, () => factory(), _sliding1Hour);

        public static Task<TItem?> GetOrCreateSliding10SecondsAsync<TItem>(object key, Func<Task<TItem?>> factory)
            => GetOrCreateAsync(key, () => factory(), _sliding2Second);

        public static Task<TItem?> GetOrCreateSliding1MinuteAsync<TItem>(object key, Func<Task<TItem?>> factory)
            => GetOrCreateAsync(key, () => factory(), _sliding1Minute);

        public static Task<TItem?> GetOrCreateSliding10MinutesAsync<TItem>(object key, Func<Task<TItem?>> factory)
            => GetOrCreateAsync(key, () => factory(), _sliding10Minutes);

        public static Task<TItem?> GetOrCreateSliding1HourAsync<TItem>(object key, Func<Task<TItem?>> factory)
            => GetOrCreateAsync(key, () => factory(), _sliding1Hour);

        public static TItem? GetOrCreateAbsolute<TItem>(object key, TimeSpan absolute, Func<TItem> factory)
            => GetOrCreate(key, () => factory(), new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = absolute
            });

        public static Task<TItem?> GetOrCreateAbsoluteAsync<TItem>(object key, TimeSpan absolute, Func<Task<TItem?>> factory)
            => GetOrCreateAsync(key, () => factory(), new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = absolute
            });

        public static T SetAbsolute1Minute<T>(object key, T value)
        {
            var absolute1Minute = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
            };
            _cache.Set(key, value, absolute1Minute);
            return value;
        }

        public static T SetAbsolute1Hour<T>(object key, T value)
        {
            var absolute1Minute = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            };
            _cache.Set(key, value, absolute1Minute);
            return value;
        }

        public static T SetSliding1Minute<T>(object key, T value)
        {
            _cache.Set(key, value, _sliding1Minute);
            return value;
        }

        public static T SetSliding10Minutes<T>(object key, T value)
        {
            _cache.Set(key, value, _sliding10Minutes);
            return value;
        }

        public static T SetSliding1Hour<T>(object key, T value)
        {
            _cache.Set(key, value, _sliding1Hour);
            return value;
        }

        /// <summary>
        /// Retrieves an item from the cache associated with the specified key, or creates and caches it using the
        /// provided factory function if it does not already exist. NULL results from the factory are not cached.
        /// </summary>
        public static TItem? GetOrCreate<TItem>(object key, Func<TItem?> factory, MemoryCacheEntryOptions entryOptions)
        {
            if (_cache.TryGetValue(key, out TItem? result))
            {
                return result;
            }

            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            semaphore.Wait();
            try
            {
                // Double-check after acquiring lock
                if (_cache.TryGetValue(key, out result))
                {
                    return result;
                }

                result = factory();
                if (result != null)
                {
                    _cache.Set(key, result, entryOptions);
                }

                return result;
            }
            finally
            {
                semaphore.Release();
                _locks.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Retrieves an item from the cache associated with the specified key, or creates and caches it using the
        /// provided factory function if it does not already exist. NULL results from the factory are not cached.
        /// </summary>
        public static async Task<TItem?> GetOrCreateAsync<TItem>(object key, Func<Task<TItem?>> factory, MemoryCacheEntryOptions entryOptions)
        {
            if (_cache.TryGetValue(key, out TItem? result))
            {
                return result;
            }

            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_cache.TryGetValue(key, out result))
                {
                    return result;
                }

                result = await factory();
                if (result != null)
                {
                    _cache.Set(key, result, entryOptions);
                }

                return result;
            }
            finally
            {
                semaphore.Release();
                _locks.TryRemove(key, out _);
            }
        }
    }
}
