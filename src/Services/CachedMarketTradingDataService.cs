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
        private readonly TimeSpan _cacheVolumeExpirationPeriod;
        private readonly TimeSpan _cacheLastTradePriceExpirationPeriod;

        public CachedMarketTradingDataService(
            IDistributedCache cache,
            IMarketTradingDataService impl,
            TimeSpan cacheVolumeExpirationPeriod,
            TimeSpan cacheLastTradePriceExpirationPeriod)
        {
            _cache = cache;
            _impl = impl;
            _cacheVolumeExpirationPeriod = cacheVolumeExpirationPeriod;
            _cacheLastTradePriceExpirationPeriod = cacheLastTradePriceExpirationPeriod;
        }

        public async Task<AssetPairTradingDataItem<double>> TryGetPairVolumeAsync(string assetPair)
        {
            var cachedItem = await _cache.TryGetFromCacheAsync(
                $"Market:TradingData:Volume:AssetPairs:{assetPair}",
                async () =>
                {
                    var item = await _impl.TryGetPairVolumeAsync(assetPair);

                    return item != null ? new CachedTradingDataItemAssetPair<double>(item) : null;
                },
                absoluteExpiration: _cacheVolumeExpirationPeriod);

            return cachedItem?.ToModel(assetPair);
        }

        public async Task<AssetPairTradingDataItem<double>> TryGetPairLastTradePriceAsync(string assetPair)
        {
            var cachedItem = await _cache.TryGetFromCacheAsync(
                $"Market:TradingData:LastTradePrice:AssetPairs:{assetPair}",
                async () =>
                {
                    var item = await _impl.TryGetPairLastTradePriceAsync(assetPair);

                    return item != null ? new CachedTradingDataItemAssetPair<double>(item) : null;
                },
                absoluteExpiration: _cacheLastTradePriceExpirationPeriod);

            return cachedItem?.ToModel(assetPair);
        }

        public async Task<IEnumerable<AssetPairTradingDataItem<double>>> TryGetAllPairsVolumeAsync()
        {
            var cachedItems = await _cache.TryGetFromCacheAsync(
                "Market:TradingData:Volume:AllPairs",
                async () =>
                {
                    var items = await _impl.TryGetAllPairsVolumeAsync();

                    return items.ToDictionary(
                        i => i.AssetPair,
                        i => new CachedTradingDataItemAssetPair<double>(i));
                },
                absoluteExpiration: _cacheVolumeExpirationPeriod);

            return cachedItems.Select(i => i.Value.ToModel(i.Key));
        }

        public async Task<IEnumerable<AssetPairTradingDataItem<double>>> TryGetAllPairsLastTradePriceAsync()
        {
            var cachedItems = await _cache.TryGetFromCacheAsync(
                "Market:TradingData:LastTradePrice:AllPairs",
                async () =>
                {
                    var items = await _impl.TryGetAllPairsLastTradePriceAsync();

                    return items.ToDictionary(
                        i => i.AssetPair,
                        i => new CachedTradingDataItemAssetPair<double>(i));
                },
                absoluteExpiration: _cacheLastTradePriceExpirationPeriod);

            return cachedItems.Select(i => i.Value.ToModel(i.Key));
        }
    }
}
