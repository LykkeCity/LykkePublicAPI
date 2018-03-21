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
            var candles = await GetLastDayCandlesAsync(assetPair); // Item1 = spot, Item2 = MT

            var volume24H = candles.Item1.History.Sum(c => c.TradingOppositeVolume) +
                            candles.Item2.History.Sum(c => c.TradingOppositeVolume);

            return new AssetPairTradingDataItem<double>(assetPair, volume24H);
        }

        public async Task<AssetPairTradingDataItem<double>> TryGetPairLastTradePriceAsync(string assetPair)
        {
            var candles = await GetLastDayCandlesAsync(assetPair); // Item1 = spot, Item2 = MT

            var lastTradePrice = candles.Item1.History.LastOrDefault()?.Close
                                 ?? candles.Item2.History.LastOrDefault()?.Close
                                 ?? 0;

            return new AssetPairTradingDataItem<double>(assetPair, lastTradePrice);
        }

        public async Task<IEnumerable<AssetPairTradingDataItem<double>>> TryGetAllPairsVolumeAsync()
        {
            var candles = await GetLastDayCandlesAsync(); // Item1 = spot, Item2 = MT

            var result = new Dictionary<string, AssetPairTradingDataItem<double>>();

            foreach (var spotAssetCandles in candles.Item1)
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

            foreach (var mtAssetCandles in candles.Item2)
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
            var candles = await GetLastDayCandlesAsync(); // Item1 = spot, Item2 = MT

            var result = new Dictionary<string, AssetPairTradingDataItem<double>>();

            foreach (var spotAssetCandles in candles.Item1)
            {
                var assetPairDataItem = new AssetPairTradingDataItem<double>(
                    spotAssetCandles.Key,
                    spotAssetCandles.Value.History.LastOrDefault()?.Close ?? 0);

                result.Add(assetPairDataItem.AssetPair, assetPairDataItem);
            }

            foreach (var mtAssetCandles in candles.Item2)
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
            Task<Tuple<IReadOnlyDictionary<string, CandlesHistoryResponseModel>,
                IReadOnlyDictionary<string, CandlesHistoryResponseModel>>> GetLastDayCandlesAsync()
        {
            var spotCandlesTask = GetLastDayCandlesAsync(MarketType.Spot);
            var mtCandlesTask = GetLastDayCandlesAsync(MarketType.Mt);

            var spotCandles = await spotCandlesTask;
            var mtCandles = await mtCandlesTask;

            return Tuple.Create(spotCandles, mtCandles);
        }

        private async Task<Tuple<CandlesHistoryResponseModel, CandlesHistoryResponseModel>> GetLastDayCandlesAsync(
            string assetPair)
        {
            var spotCandlesTask = GetLastDayCandlesAsync(MarketType.Spot, assetPair);
            var mtCandlesTask = GetLastDayCandlesAsync(MarketType.Mt, assetPair);

            var spotCandles = await spotCandlesTask;
            var mtCandles = await mtCandlesTask;

            return Tuple.Create(spotCandles, mtCandles);
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
