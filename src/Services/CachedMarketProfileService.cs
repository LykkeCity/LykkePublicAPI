using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Antares.Service.MarketProfile.LykkeClient.Models;
using Core.Services;
using Microsoft.Extensions.Caching.Distributed;
using Services.CacheModels;

namespace Services
{
    public class CachedMarketProfileService : IMarketProfileService
    {
        private readonly IMarketProfileService _impl;
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _cacheExpirationPeriod;

        public CachedMarketProfileService(
            IDistributedCache cache,
            IMarketProfileService impl, 
            TimeSpan cacheExpirationPeriod)
        {
            _impl = impl;
            _cache = cache;
            _cacheExpirationPeriod = cacheExpirationPeriod;
        }

        public async Task<AssetPairModel> TryGetPairAsync(string assetPairId)
        {
            var cachedItem = await _cache.TryGetFromCacheAsync(
                $"Market:Profile:AssetPairs:{assetPairId}", 
                async () =>
                {
                    var item = await _impl.TryGetPairAsync(assetPairId);

                    return item != null ? new CachedMarketProfileAssetPair(item) : null;
                },
                absoluteExpiration: _cacheExpirationPeriod);

            return cachedItem?.ToModel(assetPairId);
        }

        public async Task<IEnumerable<AssetPairModel>> GetAllPairsAsync()
        {
            var cachedItems = await _cache.TryGetFromCacheAsync(
                "Market:Profile:AllPairs",
                async () =>
                {
                    var items = await _impl.GetAllPairsAsync();

                    return items.ToDictionary(
                        i => i.AssetPair,
                        i => new CachedMarketProfileAssetPair(i));
                },
                absoluteExpiration: _cacheExpirationPeriod);

            return cachedItems.Select(i => i.Value.ToModel(i.Key));
        }
    }
}
