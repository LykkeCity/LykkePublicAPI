using System.Collections.Generic;
using System.Linq;
using Core.Domain.Feed;
using Core.Domain.Reports;

namespace LykkePublicAPI.Models
{
    public class ApiAssetPairRateModel
    {
        public string Id { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
    }

    public class ApiTradeVolume
    {
        public string AssetId { get; set; }
        public double Volume { get; set; }
        public int TradesCount { get; set; }
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

        public static IEnumerable<ApiTradeVolume> ToApiModel(this IEnumerable<ITradeVolumesRecord> tradeVolumes)
        {
            return tradeVolumes.Select(x => x.ToApiModel());
        }

        public static ApiTradeVolume ToApiModel(this ITradeVolumesRecord tradeVolume)
        {
            return new ApiTradeVolume
            {
                AssetId = tradeVolume.Asset,
                TradesCount = tradeVolume.TradesCount,
                Volume = tradeVolume.TotalVolume
            };
        }
    }
}
