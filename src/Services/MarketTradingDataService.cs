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
        private readonly bool _isMtEnabled;

        public MarketTradingDataService(ICandlesHistoryServiceProvider candlesHistoryServiceProvider, bool isMtEnabled)
        {
            _candlesHistoryServiceProvider = candlesHistoryServiceProvider;
            _isMtEnabled = isMtEnabled;
        }

        public async Task<AssetPairTradingDataItem<double>> TryGetPairVolumeAsync(string assetPair)
        {
            var tasks = new List<Task<CandlesHistoryResponseModel>>
            {
                GetLastEntire24HoursCandlesAsync(MarketType.Spot, assetPair),
            };
            if (_isMtEnabled)
                tasks.Add(GetLastEntire24HoursCandlesAsync(MarketType.Mt, assetPair));
            await Task.WhenAll(tasks);

            var volume24H = tasks[0].Result.History.Sum(c => c.TradingOppositeVolume);
            if (_isMtEnabled)
                volume24H += tasks[1].Result.History.Sum(c => c.TradingOppositeVolume);

            return new AssetPairTradingDataItem<double>(assetPair, volume24H);
        }

        public async Task<AssetPairTradingDataItem<double>> TryGetPairLastTradePriceAsync(string assetPair)
        {
            var spotCandle = await GetLatestCandleAsync(MarketType.Spot, assetPair);
            if (spotCandle == null && _isMtEnabled)
                spotCandle = await GetLatestCandleAsync(MarketType.Mt, assetPair);

            var lastTradePrice = spotCandle?.Close ?? 0;

            return new AssetPairTradingDataItem<double>(assetPair, lastTradePrice);
        }

        public async Task<IEnumerable<AssetPairTradingDataItem<double>>> TryGetAllPairsVolumeAsync()
        {
            var tasks = new List<Task<IReadOnlyDictionary<string, CandlesHistoryResponseModel>>>
            {
                GetLastEntire24HoursCandlesAsync(MarketType.Spot),
            };
            if (_isMtEnabled)
                tasks.Add(GetLastEntire24HoursCandlesAsync(MarketType.Mt));

            await Task.WhenAll(tasks);

            var spotCandles = tasks[0].Result;

            var result = new Dictionary<string, AssetPairTradingDataItem<double>>();

            foreach (var spotAssetCandles in spotCandles)
            {
                var volume24 = spotAssetCandles.Value.History.Sum(c => c.TradingOppositeVolume);

                if (_isMtEnabled && tasks[1].Result.TryGetValue(spotAssetCandles.Key, out var mtAssetCandles))
                    volume24 += mtAssetCandles.History.Sum(c => c.TradingOppositeVolume);

                var assetPairDataItem = new AssetPairTradingDataItem<double>(
                    spotAssetCandles.Key,
                    volume24);

                result.Add(assetPairDataItem.AssetPair, assetPairDataItem);
            }

            if (_isMtEnabled)
                foreach (var mtAssetCandles in tasks[1].Result)
                {
                    if (result.ContainsKey(mtAssetCandles.Key))
                        continue;

                    var assetPairDataItem = new AssetPairTradingDataItem<double>(
                        mtAssetCandles.Key,
                        mtAssetCandles.Value.History.Sum(c => c.TradingOppositeVolume));

                    result.Add(assetPairDataItem.AssetPair, assetPairDataItem);
                }

            return result.Values;
        }

        public async Task<IEnumerable<AssetPairTradingDataItem<double>>> TryGetAllPairsLastTradePriceAsync()
        {
            var tasks = new List<Task<IReadOnlyDictionary<string, Candle>>>
            {
                GetLatestCandlesAsync(MarketType.Spot),
            };
            if (_isMtEnabled)
                tasks.Add(GetLatestCandlesAsync(MarketType.Mt));

            await Task.WhenAll(tasks);

            var result = new Dictionary<string, AssetPairTradingDataItem<double>>();

            foreach (var spotAssetCandles in tasks[0].Result)
            {
                var candleValue = spotAssetCandles.Value;
                if (candleValue == null && _isMtEnabled)
                    tasks[1].Result.TryGetValue(spotAssetCandles.Key, out candleValue);

                var assetPairDataItem = new AssetPairTradingDataItem<double>(
                    spotAssetCandles.Key,
                    candleValue?.Close ?? 0);

                result.Add(assetPairDataItem.AssetPair, assetPairDataItem);
            }

            if (_isMtEnabled)
                foreach (var mtAssetCandles in tasks[1].Result)
                {
                    if (result.ContainsKey(mtAssetCandles.Key))
                        continue;

                    var assetPairDataItem = new AssetPairTradingDataItem<double>(
                        mtAssetCandles.Key,
                        mtAssetCandles.Value?.Close ?? 0);

                    result.Add(assetPairDataItem.AssetPair, assetPairDataItem);
                }

            return result.Values;
        }

        private async Task<IReadOnlyDictionary<string, CandlesHistoryResponseModel>> GetLastEntire24HoursCandlesAsync(MarketType market)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var now = DateTime.UtcNow;

            // Get last entire 24 hours (current hour is not entire)
            
            // inclusive
            var from = now - TimeSpan.FromHours(24);
            // exclusive
            var to = now;

            var candles = await candlesService.TryGetCandlesHistoryBatchAsync(assetPairs, CandlePriceType.Trades, CandleTimeInterval.Hour, from, to);

            return candles ?? new Dictionary<string, CandlesHistoryResponseModel>();
        }

        private async Task<CandlesHistoryResponseModel> GetLastEntire24HoursCandlesAsync(MarketType market, string assetPair)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var now = DateTime.UtcNow;

            // Get last entire 24 hours (current hour is not entire)
            
            // inclusive
            var from = now - TimeSpan.FromHours(24);
            // exclusive
            var to = now; 

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

        private async Task<IReadOnlyDictionary<string, Candle>> GetLatestCandlesAsync(MarketType market)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);
            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var now = DateTime.UtcNow;

            // Get up to 1500 candles. History depth depends on current asset pairs quantity, but
            // at least it will be 1 (current) month.

            const int maxBatchSize = 1500;
            var monthsToGet = maxBatchSize / assetPairs.Count;
            
            // inclusive
            var from = now.AddMonths(-Math.Max(0, monthsToGet - 1));
            // exclusive
            var to = now.AddMonths(1);

            var candles = await candlesService.TryGetCandlesHistoryBatchAsync(
                   assetPairs,
                    CandlePriceType.Trades,
                    CandleTimeInterval.Month,
                    from,
                    to);

            return candles?.ToDictionary(c => c.Key, c => c.Value.History.LastOrDefault()) ??
                   new Dictionary<string, Candle>();
        }

        private async Task<Candle> GetLatestCandleAsync(MarketType market, string assetPair)
        {
            var assetPairs = await GetAvailableAssetPairsAsync(market);

            if (!assetPairs.Contains(assetPair))
            {
                return null;
            }

            var candlesService = _candlesHistoryServiceProvider.Get(market);
            var now = DateTime.UtcNow;

            async Task<Candle> GetLatestCandleAsync(DateTime from, DateTime to)
            {
                var monthCandles = await candlesService.TryGetCandlesHistoryAsync(
                    assetPair,
                    CandlePriceType.Trades,
                    CandleTimeInterval.Month,
                    // inclusive
                    from,
                    // exclusive
                    to);

                return monthCandles?.History.LastOrDefault();
            }

            // Get candles for the last month. If there were no trades for the last month, then 
            // Get candles for the last 10 years + 1 month. If there were no trades for the last 10 years + 1 month, then 
            // lastTradePrice will be 0

            return await GetLatestCandleAsync(now, now.AddMonths(1)) ??
                   await GetLatestCandleAsync(now.AddYears(-10), now.AddMonths(1));
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
