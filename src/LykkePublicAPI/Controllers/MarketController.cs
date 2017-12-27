using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Feed;
using Core.Services;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.CandlesHistory.Client.Models;
using LykkePublicAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Polly;
using Services;
using MarketType = Core.Domain.Market.MarketType;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class MarketController : Controller
    {
        private readonly IDistributedCache _cache;
        private readonly IAssetPairBestPriceRepository _marketProfileRepo;
        private readonly IMarketCapitalizationService _marketCapitalizationService;
        private readonly ICandlesHistoryServiceProvider _candlesHistoryServiceProvider;

        public MarketController(
            IDistributedCache cache,
            IAssetPairBestPriceRepository marketProfileRepo,
            IMarketCapitalizationService marketCapitalizationService,
            ICandlesHistoryServiceProvider candlesHistoryServiceProvider)
        {
            _cache = cache;
            _marketProfileRepo = marketProfileRepo;
            _marketCapitalizationService = marketCapitalizationService;
            _candlesHistoryServiceProvider = candlesHistoryServiceProvider;
        }

        /// <summary>
        /// Get trade volumes for all available assetpairs
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IEnumerable<ApiMarketData>> Get()
        {
            var marketProfileTask = _marketProfileRepo.GetAsync();
            var spotCandlesTask = GetLastDayCandlesAsync(MarketType.Spot);
            var mtCandlesTask = GetLastDayCandlesAsync(MarketType.Mt);

            var marketProfile = await marketProfileTask;
            var spotCandles = await spotCandlesTask;
            var mtCandles = await mtCandlesTask;

            var result = new Dictionary<string, ApiMarketData>();

            if (marketProfile?.Profile != null)
            {
                foreach (var assetProfile in marketProfile.Profile)
                {
                    result[assetProfile.Asset] = new ApiMarketData
                    {
                        AssetPair = assetProfile.Asset,
                        Ask = assetProfile.Ask,
                        Bid = assetProfile.Bid
                    };
                }
            }

            foreach (var assetCandles in spotCandles)
            {
                if (!result.TryGetValue(assetCandles.Key, out var marketData))
                {
                    marketData = new ApiMarketData
                    {
                        AssetPair = assetCandles.Key
                    };

                    result.Add(assetCandles.Key, marketData);
                }

                marketData.LastPrice = assetCandles.Value.History.LastOrDefault()?.LastTradePrice ?? 0;
                marketData.Volume24H = assetCandles.Value.History.Sum(c => c.TradingVolume);
            }

            foreach (var assetCandles in mtCandles)
            {
                if (!result.TryGetValue(assetCandles.Key, out var marketData))
                {
                    marketData = new ApiMarketData
                    {
                        AssetPair = assetCandles.Key,
                        LastPrice = assetCandles.Value.History.LastOrDefault()?.LastTradePrice ?? 0
                    };

                    result.Add(assetCandles.Key, marketData);
                }
                
                marketData.Volume24H += assetCandles.Value.History.Sum(c => c.TradingVolume);
            }

            return result.Values;
        }

        /// <summary>
        /// Get trade volume for asset pair
        /// </summary>
        [HttpGet("{assetPair}")]
        public async Task<ApiMarketData> Get(string assetPair)
        {
            var marketProfileTask = _marketProfileRepo.GetAsync();
            var spotCandlesTask = GetLastDayCandlesAsync(MarketType.Spot, assetPair);
            var mtCandlesTask = GetLastDayCandlesAsync(MarketType.Mt, assetPair);

            var marketProfile = await marketProfileTask;
            var spotCandles = await spotCandlesTask;
            var mtCandles = await mtCandlesTask;

            var result = new ApiMarketData
            {
                AssetPair = assetPair
            };

            if (marketProfile?.Profile != null)
            {
                var assetProfile = marketProfile.Profile.FirstOrDefault(x => x.Asset == assetPair);

                result.Ask = assetProfile?.Ask ?? 0;
                result.Bid = assetProfile?.Bid ?? 0;
            }

            if (spotCandles != null)
            {
                result.LastPrice = spotCandles.History.LastOrDefault()?.LastTradePrice ?? 0;
            }
            else if(mtCandles != null)
            {
                result.LastPrice = mtCandles.History.LastOrDefault()?.LastTradePrice ?? 0;
            }

            result.Volume24H = (spotCandles?.History.Sum(c => c.TradingVolume) ?? 0) +
                               (mtCandles?.History.Sum(c => c.TradingVolume) ?? 0);

            return result;
        }

        /// <summary>
        /// Get trade volume for asset
        /// </summary>
        [HttpGet("capitalization/{market}")]
        public async Task<ApiMarketCapitalizationData> GetMarketCapitalization(string market)
        {
            var amount = await _marketCapitalizationService.GetCapitalization(market);

            return new ApiMarketCapitalizationData {Amount = amount };
        }

        private async Task<CandlesHistoryResponseModel> GetLastDayCandlesAsync(MarketType market, string assetPair)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var to = DateTime.UtcNow; // exclusive
            var from = to - TimeSpan.FromHours(25); // inclusive

            var candles = await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                    {
                        TimeSpan.Zero,
                    },
                    async (ex, ts, c) =>
                    {
                        assetPairs = await GetAvailableAssetPairsAsync(market);
                    })
                .ExecuteAsync(() =>
                {
                    if (!assetPairs.Contains(assetPair))
                    {
                        throw new InvalidOperationException($"Asset pair {assetPair} is unavailable");
                    }

                    return candlesService.TryGetCandlesHistoryAsync(assetPair, CandlePriceType.Ask, CandleTimeInterval.Hour, from, to);
                });

            return candles;
        }

        private async Task<IReadOnlyDictionary<string, CandlesHistoryResponseModel>> GetLastDayCandlesAsync(MarketType market)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var to = DateTime.UtcNow; // exclusive
            var from = to - TimeSpan.FromHours(25); // inclusive

            var candles = await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                    {
                        TimeSpan.Zero,
                    },
                    async (ex, ts, c) =>
                    {
                        assetPairs = await GetAvailableAssetPairsAsync(market);
                    })
                .ExecuteAsync(() =>
                    candlesService.TryGetCandlesHistoryBatchAsync(assetPairs, CandlePriceType.Ask, CandleTimeInterval.Hour, from, to)
                );

            return candles ?? new Dictionary<string, CandlesHistoryResponseModel>();
        }

        private Task<IList<string>> GetAvailableAssetPairsAsync(MarketType market)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromMilliseconds(50),
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(500)
                })
                .ExecuteAsync(() =>
                    _cache.TryGetFromCacheAsync(
                        $":CandlesHistory:{market}:AvailableAssetPairs",
                        () =>
                        {
                            var candlesService = _candlesHistoryServiceProvider.Get(market);

                            return Policy
                                .Handle<Exception>()
                                .WaitAndRetryAsync(new[]
                                    {
                                        TimeSpan.FromMilliseconds(100),
                                        TimeSpan.FromSeconds(1),
                                        TimeSpan.FromSeconds(10)
                                    })
                                .ExecuteAsync(() => candlesService.GetAvailableAssetPairsAsync());
                        }));
        }
    }
}
