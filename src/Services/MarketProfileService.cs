using System;
using System.Threading.Tasks;
using Core.Domain.Feed;
using Core.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Services
{
    public class MarketProfileService : IMarketProfileService
    {
        private readonly IAssetPairBestPriceRepository _assetPairBestPriceRepository;
        private readonly IMemoryCache _memoryCache;

        private const string MarketProfileCacheKey = "_MarketProfile_";
        private const string MarketProfileByPairCacheKey = "_MarketProfileByPair_{0}_";
        private readonly TimeSpan _cacheExpTime = TimeSpan.FromSeconds(2);

        public MarketProfileService(IAssetPairBestPriceRepository assetPairBestPriceRepository,
            IMemoryCache memoryCache)
        {
            _assetPairBestPriceRepository = assetPairBestPriceRepository;
            _memoryCache = memoryCache;
        }

        public async Task<MarketProfile> GetMarketProfileAsync()
        {
            MarketProfile record;

            if (!_memoryCache.TryGetValue(MarketProfileCacheKey, out record))
            {
                record = await _assetPairBestPriceRepository.GetAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_cacheExpTime);

                _memoryCache.Set(MarketProfileCacheKey, record, cacheEntryOptions);
            }

            return record;
        }

        public async Task<IFeedData> GetFeedDataAsync(string assetPairId)
        {
            IFeedData record;

            var cacheKey = GetMarketProfileCacheKey(assetPairId);

            if (!_memoryCache.TryGetValue(cacheKey, out record))
            {
                record = await _assetPairBestPriceRepository.GetAsync(assetPairId);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_cacheExpTime);

                _memoryCache.Set(cacheKey, record, cacheEntryOptions);
            }

            return record;
        }

        private static string GetMarketProfileCacheKey(string assetPairId)
        {
            return string.Format(MarketProfileByPairCacheKey, assetPairId);
        }
    }
}
