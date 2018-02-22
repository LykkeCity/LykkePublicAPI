using System;
using System.Threading.Tasks;
using Core.Services;
using Microsoft.Extensions.Caching.Distributed;
using Services.CacheModels;

namespace Services
{
    public class CachedMarketCapitalizationService : IMarketCapitalizationService
    {
        private readonly IDistributedCache _cache;
        private readonly IMarketCapitalizationService _impl;
        private readonly TimeSpan _cacheExpirationPeriod;

        public CachedMarketCapitalizationService(
            IDistributedCache cache,
            IMarketCapitalizationService impl,
            TimeSpan cacheExpirationPeriod)
        {
            _cache = cache;
            _impl = impl;
            _cacheExpirationPeriod = cacheExpirationPeriod;
        }

        public async Task<double?> GetCapitalization(string market)
        {
            var cachedItem = await _cache.TryGetFromCacheAsync(
                $"Market:Capitalization:AssetPairs:{market}",
                async () =>
                {
                    var capitalization = await _impl.GetCapitalization(market);

                    return capitalization.HasValue ? new CachedMarketCapitalizationAssetPair(capitalization.Value) : null;
                },
                absoluteExpiration: _cacheExpirationPeriod);

            return cachedItem?.Capitalization;
        }
    }
}
