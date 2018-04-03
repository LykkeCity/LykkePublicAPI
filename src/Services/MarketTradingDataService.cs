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
            var candles = await GetLastCandleAsync(assetPair); // Getting candles for the last hour

            var lastTradePrice = candles.spotCandle?.Close
                                 ?? candles.mtCandle?.Close
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

                if (candles.mtCandles.TryGetValue(spotAssetCandles.Key, out var mtAssetCandles))
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
            var candles = await GetLastCandlesAsync(); // Getting candles for the last hour

            var result = new Dictionary<string, AssetPairTradingDataItem<double>>();

            foreach (var spotAssetCandles in candles.spotCandles)
            {
                var assetPairDataItem = new AssetPairTradingDataItem<double>(
                    spotAssetCandles.Key,
                    spotAssetCandles.Value?.Close ?? 0);

                result.Add(assetPairDataItem.AssetPair, assetPairDataItem);
            }

            foreach (var mtAssetCandles in candles.mtCandles)
            {
                if (!result.ContainsKey(mtAssetCandles.Key))
                {
                    var assetPairDataItem = new AssetPairTradingDataItem<double>(
                        mtAssetCandles.Key,
                        mtAssetCandles.Value?.Close ?? 0);

                    result.Add(assetPairDataItem.AssetPair, assetPairDataItem);
                }
            }

            return result.Values;
        }

        private async
            Task<(IReadOnlyDictionary<string, CandlesHistoryResponseModel> spotCandles,
                IReadOnlyDictionary<string, CandlesHistoryResponseModel> mtCandles)> GetLastDayCandlesAsync()
        {
            var spotCandlesTask = GetLastDayCandlesAsync(MarketType.Spot);
            var mtCandlesTask = GetLastDayCandlesAsync(MarketType.Mt);

            var spotCandlesData = await spotCandlesTask;
            var mtCandlesData = await mtCandlesTask;

            return (spotCandles: spotCandlesData, mtCandles: mtCandlesData);
        }

        private async Task<(CandlesHistoryResponseModel spotCandles, CandlesHistoryResponseModel mtCandles)> GetLastDayCandlesAsync(
            string assetPair)
        {
            var spotCandlesTask = GetLastDayCandlesAsync(MarketType.Spot, assetPair);
            var mtCandlesTask = GetLastDayCandlesAsync(MarketType.Mt, assetPair);

            var spotCandlesData = await spotCandlesTask;
            var mtCandlesData = await mtCandlesTask;

            return (spotCandles: spotCandlesData, mtCandles: mtCandlesData);
        }

        private async Task<IReadOnlyDictionary<string, CandlesHistoryResponseModel>> GetLastDayCandlesAsync(MarketType market)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var to = DateTime.UtcNow; // exclusive
            var from = to - TimeSpan.FromHours(24); // inclusive

            var candles = await candlesService.TryGetCandlesHistoryBatchAsync(assetPairs, CandlePriceType.Trades, CandleTimeInterval.Hour, from, to);

            return candles ?? new Dictionary<string, CandlesHistoryResponseModel>();
        }

        private async Task<CandlesHistoryResponseModel> GetLastDayCandlesAsync(MarketType market, string assetPair)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var to = DateTime.UtcNow; // exclusive
            var from = to - TimeSpan.FromHours(24); // inclusive

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

        private async
            Task<(IReadOnlyDictionary<string, Candle> spotCandles,
                IReadOnlyDictionary<string, Candle> mtCandles)> GetLastCandlesAsync()
        {
            var spotCandlesTask = GetLastCandlesAsync(MarketType.Spot);
            var mtCandlesTask = GetLastCandlesAsync(MarketType.Mt);

            var spotCandlesData = await spotCandlesTask;
            var mtCandlesData = await mtCandlesTask;

            return (spotCandles: spotCandlesData, mtCandles: mtCandlesData);
        }

        private async Task<(Candle spotCandle, Candle mtCandle)> GetLastCandleAsync(
            string assetPair)
        {
            var spotCandlesTask = GetLastCandleAsync(MarketType.Spot, assetPair);
            var mtCandlesTask = GetLastCandleAsync(MarketType.Mt, assetPair);

            var spotCandlesData = await spotCandlesTask;
            var mtCandlesData = await mtCandlesTask;

            return (spotCandle: spotCandlesData, mtCandle: mtCandlesData);
        }

        private async Task<IReadOnlyDictionary<string, Candle>> GetLastCandlesAsync(MarketType market)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var from = DateTime.UtcNow; // inclusive
            var to = from.AddHours(1); // exclusive
            
            var candles = await candlesService.TryGetCandlesHistoryBatchAsync(assetPairs, CandlePriceType.Trades, CandleTimeInterval.Hour, from, to);

            return candles?.ToDictionary(c => c.Key, c => c.Value.History.SingleOrDefault())
                   ?? new Dictionary<string, Candle>();
        }

        private async Task<Candle> GetLastCandleAsync(MarketType market, string assetPair)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var from = DateTime.UtcNow; // inclusive
            var to = from.AddHours(1); // exclusive

            if (!assetPairs.Contains(assetPair))
            {
                return null;
            }

            var candles = await candlesService.TryGetCandlesHistoryAsync(assetPair, CandlePriceType.Trades, CandleTimeInterval.Hour, from, to);

            return candles?.History.SingleOrDefault();
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
