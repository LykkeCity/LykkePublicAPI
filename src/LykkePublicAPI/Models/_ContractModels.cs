using System.Collections.Generic;
using System.Linq;
using Core.Domain.Exchange;
using Core.Domain.Feed;

namespace LykkePublicAPI.Models
{
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
    }
}
