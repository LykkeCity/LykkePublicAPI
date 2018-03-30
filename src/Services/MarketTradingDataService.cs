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
        
        public async Task<AssetPairTradingDataItem<double>> TryGetPairVolumeAsync(string assetPair)
        {
            var candles = await GetLastDayCandlesAsync(assetPair);

            var volume24H = candles.spotCandles.History.Sum(c => c.TradingOppositeVolume) +
                            candles.mtCandles.History.Sum(c => c.TradingOppositeVolume);

            return new AssetPairTradingDataItem<double>(assetPair, volume24H);
        }

        public async Task<AssetPairTradingDataItem<double>> TryGetPairLastTradePriceAsync(string assetPair)
        {
            var candles = await GetLastDayCandlesAsync(assetPair, 1); // Getting candles for the last hour

            var lastTradePrice = candles.spotCandles.History.LastOrDefault()?.Close
                                 ?? candles.mtCandles.History.LastOrDefault()?.Close
                                 ?? 0;

            return new AssetPairTradingDataItem<double>(assetPair, lastTradePrice);
        }

        public async Task<IEnumerable<AssetPairTradingDataItem<double>>> TryGetAllPairsVolumeAsync()
        {
            var candles = await GetLastDayCandlesAsync();

            var result = new Dictionary<string, AssetPairTradingDataItem<double>>();

            foreach (var spotAssetCandles in candles.spotCandles)
            {
                var volume24 = spotAssetCandles.Value.History.Sum(c => c.TradingOppositeVolume);

                if (candles.Item2.TryGetValue(spotAssetCandles.Key, out var mtAssetCandles))
                {
                    volume24 += mtAssetCandles.History.Sum(c => c.TradingOppositeVolume);
                }

                var assetPairDataItem = new AssetPairTradingDataItem<double>(
                    spotAssetCandles.Key,
                    volume24);

                result.Add(assetPairDataItem.AssetPair, assetPairDataItem);
            }

            foreach (var mtAssetCandles in candles.mtCandles)
            {
                if (!result.ContainsKey(mtAssetCandles.Key))
                {
                    var assetPairDataItem = new AssetPairTradingDataItem<double>(
                        mtAssetCandles.Key,
                        mtAssetCandles.Value.History.Sum(c => c.TradingOppositeVolume));

                    result.Add(assetPairDataItem.AssetPair, assetPairDataItem);
                }
            }

            return result.Values;
        }

        public async Task<IEnumerable<AssetPairTradingDataItem<double>>> TryGetAllPairsLastTradePriceAsync()
        {
            var candles = await GetLastDayCandlesAsync(1); // Getting candles for the last hour

            var result = new Dictionary<string, AssetPairTradingDataItem<double>>();

            foreach (var spotAssetCandles in candles.spotCandles)
            {
                var assetPairDataItem = new AssetPairTradingDataItem<double>(
                    spotAssetCandles.Key,
                    spotAssetCandles.Value.History.LastOrDefault()?.Close ?? 0);

                result.Add(assetPairDataItem.AssetPair, assetPairDataItem);
            }

            foreach (var mtAssetCandles in candles.mtCandles)
            {
                if (!result.ContainsKey(mtAssetCandles.Key))
                {
                    var assetPairDataItem = new AssetPairTradingDataItem<double>(
                        mtAssetCandles.Key,
                        mtAssetCandles.Value.History.LastOrDefault()?.Close ?? 0);

                    result.Add(assetPairDataItem.AssetPair, assetPairDataItem);
                }
            }

            return result.Values;
        }

        private async
            Task<(IReadOnlyDictionary<string, CandlesHistoryResponseModel> spotCandles,
                IReadOnlyDictionary<string, CandlesHistoryResponseModel> mtCandles)> GetLastDayCandlesAsync(int periodInHours = 24)
        {
            var spotCandlesTask = GetLastDayCandlesAsync(MarketType.Spot, periodInHours);
            var mtCandlesTask = GetLastDayCandlesAsync(MarketType.Mt, periodInHours);

            var spotCandlesData = await spotCandlesTask;
            var mtCandlesData = await mtCandlesTask;

            return (spotCandles: spotCandlesData, mtCandles: mtCandlesData);
        }

        private async Task<(CandlesHistoryResponseModel spotCandles, CandlesHistoryResponseModel mtCandles)> GetLastDayCandlesAsync(
            string assetPair, int periodInHours = 24)
        {
            var spotCandlesTask = GetLastDayCandlesAsync(MarketType.Spot, assetPair, periodInHours);
            var mtCandlesTask = GetLastDayCandlesAsync(MarketType.Mt, assetPair, periodInHours);

            var spotCandlesData = await spotCandlesTask;
            var mtCandlesData = await mtCandlesTask;

            return (spotCandles: spotCandlesData, mtCandles: mtCandlesData);
        }

        private async Task<IReadOnlyDictionary<string, CandlesHistoryResponseModel>> GetLastDayCandlesAsync(MarketType market, int periodInHours = 24)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var to = DateTime.UtcNow; // exclusive
            var from = to - TimeSpan.FromHours(periodInHours + 1); // inclusive

            var candles = await candlesService.TryGetCandlesHistoryBatchAsync(assetPairs, CandlePriceType.Trades, CandleTimeInterval.Hour, from, to);

            return candles ?? new Dictionary<string, CandlesHistoryResponseModel>();
        }

        private async Task<CandlesHistoryResponseModel> GetLastDayCandlesAsync(MarketType market, string assetPair, int periodInHours = 24)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var to = DateTime.UtcNow; // exclusive
            var from = to - TimeSpan.FromHours(periodInHours + 1); // inclusive

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
