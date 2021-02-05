using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Service.MarketProfile.LykkeClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Core.Domain.Exchange;
using Core.Feed;
using Lykke.Domain.Prices.Model;
using Lykke.Domain.Prices.Contracts;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.CandlesHistory.Client.Models;
using MessagePack;

namespace LykkePublicAPI.Models
{
    public class ApiAssetPair
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public int Accuracy { get; set; }
        public int InvertedAccuracy { get; set; }

        public string BaseAssetId { get; set; }
        public string QuotingAssetId { get; set; }
    }

    public class ApiAsset
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string DisplayId { get; set; }

        public string BitcoinAssetId { get; set; }
        public string BitcoinAssetAddress { get; set; }

        public string Symbol { get; set; }

        public int Accuracy { get; set; }
    }

    public class ApiAssetPairRateModel
    {
        public string Id { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
    }

    public class ApiAssetPairHistoryRateModel
    {
        public string Id { get; set; }
        public double? Bid { get; set; }
        public double? Ask { get; set; }
        public double? TradingVolume { get; set; }
        public double? TradingOppositeVolume { get; set; }
    }

    [MessagePackObject]
    public class ApiMarketData
    {
        [Key(0)]
        public string AssetPair { get; set; }

        [Key(1)]
        public double Volume24H { get; set; }

        [Key(2)]
        public double LastPrice { get; set; }

        [Key(3)]
        public double Bid { get; set; }

        [Key(4)]
        public double Ask { get; set; }
    }

    [MessagePackObject]
    public class ApiMarketCapitalizationData
    {
        [Key(0)]
        public double Amount { get; set; }
    }

    public class CandleWithPairId
    {
        public string AssetPairId { get; set; }
        public IFeedCandle Candle { get; set; }
    }

    public class ApiCandle
    {
        public DateTime T { get; set; }
        public double O { get; set; }
        public double C { get; set; }
        public double H { get; set; }
        public double L { get; set; }
        public double V { get; set; }
        public double OV { get; set; }
    }

    public class ApiCandle2
    {
        public DateTime DateTime { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Volume { get; set; }
        public double OppositeVolume { get; set; }
    }

    public class CandlesHistoryResponse<T>
    {
        public string AssetPair { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Period Period { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PriceType Type { get; set; }
        public List<T> Data { get; set; } = new List<T>();
    }

    public class CandlesHistoryListResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
    }

    public class ApiCommonTrade
    {
        public string Id { get; set; }
        public DateTime Dt { get; set; }
        public string BaseAssetId { get; set; }
        public string QuotingAssetId { get; set; }
        public string AssetPair { get; set; }
        public double Price { get; set; }
        public double Amount { get; set; }
    }

    public enum ErrorCodes
    {
        InvalidInput = 1
    }

    public class ApiError
    {
        public ErrorCodes Code { get; set; }
        public string Msg { get; set; }
    }

    public static class Convertions
    {
        public static ApiAssetPairRateModel ToApiModel(this AssetPairModel feedData)
        {
            return new ApiAssetPairRateModel
            {
                Ask = feedData.AskPrice,
                Bid = feedData.BidPrice,
                Id = feedData.AssetPair
            };
        }

        public static ApiCommonTrade ToApiModel(this ITradeCommon trade)
        {
            return new ApiCommonTrade
            {
                Amount = trade.Amount,
                BaseAssetId = trade.BaseAsset,
                Dt = trade.Dt,
                Id = trade.Id,
                Price = trade.Price,
                QuotingAssetId = trade.QuotAsset,
                AssetPair = trade.AssetPair
            };
        }

        public static IEnumerable<ApiCommonTrade> ToApiModel(this IEnumerable<ITradeCommon> trades)
        {
            return trades.Select(x => x.ToApiModel());
        }

        public static ApiAssetPair ToApiModel(this AssetPair assetPair)
        {
            return new ApiAssetPair
            {
                Accuracy = assetPair.Accuracy,
                BaseAssetId = assetPair.BaseAssetId,
                Id = assetPair.Id,
                InvertedAccuracy = assetPair.InvertedAccuracy,
                Name = assetPair.Name,
                QuotingAssetId = assetPair.QuotingAssetId
            };
        }

        public static IEnumerable<ApiAssetPair> ToApiModel(this IEnumerable<AssetPair> assetPairs)
        {
            return assetPairs.Select(x => x.ToApiModel());
        }

        public static ApiAssetPairHistoryRateModel ToApiModel(string assetPairId, Candle buyCandle, Candle sellCandle, Candle tradeCandle)
        {
            double? GetVolume(double? buyVolume, double? askVolume, double? tradeVolume)
            {
                var result = buyVolume;

                if (askVolume.HasValue)
                {
                    result = result.HasValue ? Math.Max(result.Value, askVolume.Value) : askVolume;
                }
                if (tradeVolume.HasValue)
                {
                    result = result.HasValue ? Math.Max(result.Value, tradeVolume.Value) : tradeVolume;
                }

                return result;
            }

            return new ApiAssetPairHistoryRateModel
            {
                Id = assetPairId,
                Ask = sellCandle?.Close,
                Bid = buyCandle?.Close,
                TradingVolume = GetVolume(buyCandle?.TradingVolume, sellCandle?.TradingVolume, tradeCandle?.TradingVolume),
                TradingOppositeVolume = GetVolume(buyCandle?.TradingOppositeVolume, sellCandle?.TradingOppositeVolume, tradeCandle?.TradingOppositeVolume)
            };
        }

        private static ApiAssetPairHistoryRateModel ToApiModel(string assetPairId, IFeedCandle buyCandle, IFeedCandle sellCandle)
        {
            return new ApiAssetPairHistoryRateModel
            {
                Id = assetPairId,
                Ask = sellCandle?.Close,
                Bid = buyCandle?.Close
            };
        }

        public static IEnumerable<ApiAssetPairHistoryRateModel> ToApiModel(this IEnumerable<CandleWithPairId> candles)
        {
            var grouped = candles.GroupBy(x => x.AssetPairId, x => x.Candle);

            var result = new List<ApiAssetPairHistoryRateModel>();

            foreach (var group in grouped)
            {
                var buyCandle = group.First(x => x.IsBuy);
                var sellCandle = group.First(x => !x.IsBuy);

                result.Add(ToApiModel(group.Key, buyCandle, sellCandle));
            }

            return result;
        }

        public static ApiCandle ToApiCandle(this Candle candle)
        {
            return (candle != null) ? new ApiCandle
            {
                T = candle.DateTime,
                O = candle.Open,
                C = candle.Close,
                H = candle.High,
                L = candle.Low,
                V = candle.TradingVolume,
                OV = candle.TradingOppositeVolume
            } : null;
        }

        public static ApiCandle2 ToApiCandle2(this Candle candle)
        {
            return (candle != null) ? new ApiCandle2
            {
                DateTime = candle.DateTime,
                Open = candle.Open,
                Close = candle.Close,
                High = candle.High,
                Low = candle.Low,
                Volume = candle.TradingVolume,
                OppositeVolume = candle.TradingOppositeVolume
            } : null;
        }

        public static IEnumerable<ApiCandle> ToApiModel(this IEnumerable<Candle> candles)
        {
            if (candles != null)
            {
                foreach (var candle in candles)
                {
                    yield return candle.ToApiCandle();
                }
            }
        }

        public static IEnumerable<ApiCandle2> ToApiModel2(this IEnumerable<Candle> candles)
        {
            if (candles != null)
            {
                foreach (var candle in candles)
                {
                    yield return candle.ToApiCandle2();
                }
            }
        }

        public static ApiAsset ToApiModel(this Asset asset)
        {
            return new ApiAsset
            {
                Accuracy = asset.Accuracy,
                BitcoinAssetAddress = asset.AssetAddress,
                BitcoinAssetId = asset.BlockChainAssetId,
                Id = asset.Id,
                Name = asset.Name,
                Symbol = asset.Symbol,
                DisplayId = asset.DisplayId
            };
        }

        public static IEnumerable<ApiAsset> ToApiModel(this IEnumerable<Asset> assets)
        {
            return assets.Select(x => x.ToApiModel());
        }

        public static CandleWithPairId ToCandleWithPairId(this IFeedHistory feedHistory)
        {
            return new CandleWithPairId
            {
                AssetPairId = feedHistory.AssetPair,
                Candle = new FeedCandle
                {
                    DateTime = feedHistory.FeedTime,
                    Close = feedHistory.TradeCandles.Last().Close,
                    High = feedHistory.TradeCandles.Last().High,
                    IsBuy = feedHistory.PriceType == TradePriceType.Bid,
                    Low = feedHistory.TradeCandles.Last().Low,
                    Open = feedHistory.TradeCandles.Last().Open
                }
            };
        }
    }
}
