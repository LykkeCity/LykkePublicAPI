using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Market;
using Core.Services;
using Microsoft.Extensions.Caching.Distributed;
using Services.CacheModels;

namespace Services
{
    public class CachedMarketTradingDataService : IMarketTradingDataService
    {
        private readonly IDistributedCache _cache;
        private readonly IMarketTradingDataService _impl;
        private readonly TimeSpan _cacheExpirationPeriod;

        public CachedMarketTradingDataService(
            IDistributedCache cache,
            IMarketTradingDataService impl,
            TimeSpan cacheExpirationPeriod)
        {
            _cache = cache;
            _impl = impl;
            _cacheExpirationPeriod = cacheExpirationPeriod;
        }

        public async Task<AssetPairTradingData> TryGetPairAsync(string assetPair)
        {
            var cachedItem = await _cache.TryGetFromCacheAsync(
                $"Market:TradingData:AssetPairs:{assetPair}",
                async () =>
                {
                    var item = await _impl.TryGetPairAsync(assetPair);

                    return item != null ? new CachedTradingDataAssetPair(item) : null;
                },
                absoluteExpiration: _cacheExpirationPeriod);

            return cachedItem?.ToModel(assetPair);
        }

        public async Task<IEnumerable<AssetPairTradingData>> GetAllPairsAsync()
        {
            var cachedItems = await _cache.TryGetFromCacheAsync(
                "Market:TradingData:AllPairs",
                async () =>
                {
                    var items = await _impl.GetAllPairsAsync();

                    return items.ToDictionary(
                        i => i.AssetPair,
                        i => new CachedTradingDataAssetPair(i));
                },
                absoluteExpiration: _cacheExpirationPeriod);

            return cachedItems.Select(i => i.Value.ToModel(i.Key));
        }
    }
}
