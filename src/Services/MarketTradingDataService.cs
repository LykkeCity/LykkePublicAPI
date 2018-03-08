using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Market;
using Core.Services;
using Lykke.Service.CandlesHistory.Client;
using Lykke.Service.CandlesHistory.Client.Models;
using Polly;

namespace Services
{
    public class MarketTradingDataService : IMarketTradingDataService
    {
        private readonly ICandlesHistoryServiceProvider _candlesHistoryServiceProvider;

        public MarketTradingDataService(ICandlesHistoryServiceProvider candlesHistoryServiceProvider)
        {
            _candlesHistoryServiceProvider = candlesHistoryServiceProvider;
        }
        
        public async Task<AssetPairTradingData> TryGetPairAsync(string assetPair)
        {
            var spotCandlesTask = GetLastDayCandlesAsync(MarketType.Spot, assetPair);
            var mtCandlesTask = GetLastDayCandlesAsync(MarketType.Mt, assetPair);

            var spotCandles = await spotCandlesTask;
            var mtCandles = await mtCandlesTask;

            var lastTradePrice = spotCandles.History.LastOrDefault()?.LastTradePrice
                            ?? mtCandles.History.LastOrDefault()?.LastTradePrice
                            ?? 0;

            var volume24H = spotCandles.History.Sum(c => c.TradingOppositeVolume) +
                            mtCandles.History.Sum(c => c.TradingOppositeVolume);

            return new AssetPairTradingData(assetPair, lastTradePrice, volume24H);
        }

        public async Task<IEnumerable<AssetPairTradingData>> GetAllPairsAsync()
        {
            var spotCandlesTask = GetLastDayCandlesAsync(MarketType.Spot);
            var mtCandlesTask = GetLastDayCandlesAsync(MarketType.Mt);

            var spotCandles = await spotCandlesTask;
            var mtCandles = await mtCandlesTask;

            var result = new Dictionary<string, AssetPairTradingData>();

            foreach (var spotAssetCandles in spotCandles)
            {
                var volume24 = spotAssetCandles.Value.History.Sum(c => c.TradingOppositeVolume);

                if (mtCandles.TryGetValue(spotAssetCandles.Key, out var mtAssetCandles))
                {
                    volume24 += mtAssetCandles.History.Sum(c => c.TradingOppositeVolume);
                }

                var assetPairData = new AssetPairTradingData(
                    spotAssetCandles.Key,
                    spotAssetCandles.Value.History.LastOrDefault()?.LastTradePrice ?? 0,
                    volume24);

                result.Add(assetPairData.AssetPair, assetPairData);
            }

            foreach (var mtAssetCandles in mtCandles)
            {
                if (!result.ContainsKey(mtAssetCandles.Key))
                {
                    var assetPairData = new AssetPairTradingData(
                        mtAssetCandles.Key,
                        mtAssetCandles.Value.History.LastOrDefault()?.LastTradePrice ?? 0,
                        mtAssetCandles.Value.History.Sum(c => c.TradingOppositeVolume));

                    result.Add(assetPairData.AssetPair, assetPairData);
                }
            }

            return result.Values;
        }

        private async Task<IReadOnlyDictionary<string, CandlesHistoryResponseModel>> GetLastDayCandlesAsync(MarketType market)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var to = DateTime.UtcNow; // exclusive
            var from = to - TimeSpan.FromHours(25); // inclusive

            var candles = await candlesService.TryGetCandlesHistoryBatchAsync(assetPairs, CandlePriceType.Trades, CandleTimeInterval.Hour, from, to);

            return candles ?? new Dictionary<string, CandlesHistoryResponseModel>();
        }

        private async Task<CandlesHistoryResponseModel> GetLastDayCandlesAsync(MarketType market, string assetPair)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var to = DateTime.UtcNow; // exclusive
            var from = to - TimeSpan.FromHours(25); // inclusive

            if (!assetPairs.Contains(assetPair))
            {
                return new CandlesHistoryResponseModel
                {
                    History = new List<Candle>()
                };
            }

            var candles = await candlesService.TryGetCandlesHistoryAsync(assetPair, CandlePriceType.Trades, CandleTimeInterval.Hour, from, to);

            return candles;
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
                });
        }
    }
}
