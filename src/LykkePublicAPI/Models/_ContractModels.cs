using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Core.Domain.Assets;
using Core.Domain.Candles;
using Core.Domain.Exchange;
using Core.Domain.Feed;

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

    public class ApiAssetPairRateModel
    {
        public string Id { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
    }

    public class ApiMarketData
    {
        public string AssetPair { get; set; }
        public double Volume24H { get; set; }
        public double LastPrice { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
    }

    public class CandleWithPairId
    {
        public string AssetPairId { get; set; }
        public IFeedCandle Candle { get; set; }
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
        public static IEnumerable<ApiAssetPairRateModel> ToApiModel(this MarketProfile marketProfile)
        {
            return marketProfile.Profile.Select(x => x.ToApiModel());
        }

        public static ApiAssetPairRateModel ToApiModel(this IFeedData feedData)
        {
            return new ApiAssetPairRateModel
            {
                Ask = feedData.Ask,
                Bid = feedData.Bid,
                Id = feedData.Asset
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

        public static ApiAssetPairRateModel ToApiModel(string assetPairId, IFeedCandle buyCandle, IFeedCandle sellCandle)
        {
            return new ApiAssetPairRateModel
            {
                Id = assetPairId,
                Ask = sellCandle.Close,
                Bid = buyCandle.Close
            };
        }

        public static IEnumerable<ApiAssetPairRateModel> ToApiModel(this IEnumerable<CandleWithPairId> candles)
        {
            var grouped = candles.GroupBy(x => x.AssetPairId, x => x.Candle);

            var result = new List<ApiAssetPairRateModel>();

            foreach (var group in grouped)
            {
                var buyCandle = group.First(x => x.IsBuy);
                var sellCandle = group.First(x => !x.IsBuy);

                result.Add(ToApiModel(group.Key, buyCandle, sellCandle));
            }

            return result;
        }
    }
}
