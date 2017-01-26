using System;
using Core.Domain.Candles;

namespace LykkePublicAPI.Models
{
    public enum Period
    {
        Sec,
        Minute,
        Hour,
        Day,
        Month
    }

    public class AssetPairsRateHistoryRequest
    {
        public string[] AssetPairIds { get; set; }
        public Period Period { get; set; }
        public DateTime DateTime { get; set; }
    }

    public class AssetPairRateHistoryRequest
    {
        public Period Period { get; set; }
        public DateTime DateTime { get; set; }
    }

    public static class RequestsConvertorsExt
    {
        public static FeedCandleType ToDomainModel(this Period candleType)
        {
            switch (candleType)
            {
                case Period.Sec:
                    return FeedCandleType.Sec;
                case Period.Minute:
                    return FeedCandleType.Minute;
                case Period.Hour:
                    return FeedCandleType.Hour;
                case Period.Day:
                    return FeedCandleType.Day;
                case Period.Month:
                    return FeedCandleType.Month;
                default:
                    throw new ArgumentOutOfRangeException(nameof(candleType), candleType, null);
            }
        }
    }
}
