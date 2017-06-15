using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Core.Domain.Assets;
using Core.Domain.Exchange;
using Core.Domain.Feed;
using Core.Feed;
using Lykke.Domain.Prices.Model;
using Lykke.Domain.Prices.Contracts;
using Lykke.MarketProfileService.Client.Models;

namespace LykkePublicAPI.Models
{
    public class ApiDutchAuctionOrderbook
    {
        public double Price { get; set; }
        public double InMoneyVolume { get; set; }
        public double OutOfTheMoneyVolume { get; set; }
        public IEnumerable<Order> InMoneyOrders { get; set; }
        public IEnumerable<Order> OutOfTheMoneyOrders { get; set; }

        public class Order
        {
            public double Price { get; set; }
            public double Volume { get; set; }
            public int Investors { get; set; }
        }
    }

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
    }

    public class ApiMarketData
    {
        public string AssetPair { get; set; }
        public double Volume24H { get; set; }
        public double LastPrice { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
    }

    public class ApiMarketCapitalizationData
    {
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
    }

    public class CandlesHistoryResponse
    {
        public string AssetPair { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Period Period { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PriceType Type { get; set; }
        public List<ApiCandle> Data { get; set; } = new List<ApiCandle>();
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

        public static ApiMarketData ToApiModel(this IMarketData marketData, IFeedData feedData)
        {
            return new ApiMarketData
            {
                Ask = feedData.Ask,
                AssetPair = marketData.AssetPairId,
                Bid = feedData.Bid,
                LastPrice = marketData.LastPrice,
                Volume24H = marketData.Volume
            };
        }

        public static IEnumerable<ApiMarketData> ToApiModel(this IEnumerable<IMarketData> marketData,
            MarketProfile marketProfile)
        {
            return marketData.Select(x => x.ToApiModel(marketProfile.Profile.First(y => y.Asset == x.AssetPairId)));
        }

        public static ApiAssetPair ToApiModel(this IAssetPair assetPair)
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

        public static IEnumerable<ApiAssetPair> ToApiModel(this IEnumerable<IAssetPair> assetPairs)
        {
            return assetPairs.Select(x => x.ToApiModel());
        }

        public static ApiAssetPairHistoryRateModel ToApiModel(string assetPairId, IFeedCandle buyCandle, IFeedCandle sellCandle)
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

        public static ApiCandle ToApiCandle(this IFeedCandle candle)
        {
            return (candle != null) ? new ApiCandle
            {
                T = candle.DateTime,
                O = candle.Open,
                C = candle.Close,
                H = candle.High,
                L = candle.Low
            } : null;
        }

        public static IEnumerable<ApiCandle> ToApiModel(this IEnumerable<IFeedCandle> candles)
        {
            if (candles != null)
            {
                foreach (var candle in candles)
                {
                    yield return candle.ToApiCandle();
                }
            }
            yield break;
        }

        public static ApiAsset ToApiModel(this IAsset asset)
        {
            return new ApiAsset
            {
                Accuracy = asset.Accuracy,
                BitcoinAssetAddress = asset.AssetAddress,
                BitcoinAssetId = asset.BlockChainAssetId,
                Id = asset.Id,
                Name = asset.Name,
                Symbol = asset.Symbol
            };
        }

        public static IEnumerable<ApiAsset> ToApiModel(this IEnumerable<IAsset> assets)
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
