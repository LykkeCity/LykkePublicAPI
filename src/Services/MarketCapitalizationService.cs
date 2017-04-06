using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Accounts;
using Core.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Services
{
    class CacheRecord
    {
        public DateTime Dt { get; set; }
        public string AssetId { get; set; }
        public double Amount { get; set; }
    }

    public class MarketCapitalizationService : IMarketCapitalizationService
    {
        private const string MarketCapitalizationCacheKey = "_MarketCapital_{0}";
        private const string LKK = "LKK";
        private readonly TimeSpan _cacheExpTime = TimeSpan.FromMinutes(10);

        private readonly IWalletsRepository _walletsRepository;
        private readonly IMemoryCache _memCache;

        public MarketCapitalizationService(IWalletsRepository walletsRepository,
            IMemoryCache memCache)
        {
            _walletsRepository = walletsRepository;
            _memCache = memCache;
        }

        public async Task<double> GetCapitalization(string market)
        {
            if (market == LKK) //ToDo: extend to all markets. Remove hardcode
            {
                CacheRecord record;

                if (!_memCache.TryGetValue(MarketCapitalizationCacheKey, out record))
                {
                    double amount = 0;

                    await _walletsRepository.GetWalletsByChunkAsync(pairs =>
                    {
                        try
                        {
                            var c = pairs.Select(x => x.Value?.FirstOrDefault(y => y.AssetId == LKK)).Sum(x => x?.Balance ?? 0);
                            amount += c;

                        }
                        catch (Exception ex)
                        {
                            
                        }
                        return Task.CompletedTask;
                    });

                    record = record ?? new CacheRecord();

                    record.AssetId = LKK;
                    record.Dt = DateTime.UtcNow;
                    record.Amount = amount;

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(_cacheExpTime);

                    _memCache.Set(MarketCapitalizationCacheKey, record, cacheEntryOptions);
                }

                return record.Amount;
            }

            return 0;
        }
    }
}
