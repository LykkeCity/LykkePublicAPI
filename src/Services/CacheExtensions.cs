using System;
using System.IO;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Caching.Distributed;

namespace Services
{
    public static class CacheExtensions
    {
        // TODO: Add update predicate
        // TODO: Make atomic
        public static async Task<T> TryGetFromCacheAsync<T>(this IDistributedCache cache, string key,
            Func<Task<T>> getRecordFunc, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            var record = await cache.TryGetFromCacheAsync<T>(key);

            if (record == null)
            {
                record = await getRecordFunc();
                await cache.UpdateCacheAsync(key, record, absoluteExpiration, slidingExpiration);
            }

            return record;
        }

        public static async Task<T> TryGetFromCacheAsync<T>(this IDistributedCache cache, string key)
        {
            var value = await cache.GetAsync(key);

            if (value != null)
            {
                using (var stream = new MemoryStream(value))
                {
                    return MessagePackSerializer.Deserialize<T>(stream, StandardResolverAllowPrivate.Instance);
                }
            }

            return default(T);
        }

        public static async Task UpdateCacheAsync<T>(this IDistributedCache cache, string key, T record, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            var value = MessagePackSerializer.Serialize(record);

            await cache.SetAsync(key, value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration,
                SlidingExpiration = slidingExpiration
            });
        }
    }
}
