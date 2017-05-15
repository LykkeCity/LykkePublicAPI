using System;
using System.ComponentModel.DataAnnotations;
using Lykke.Domain.Prices;

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

    public enum PriceType
    {
        Bid = 1,
        Ask = 2,
        Mid = 3
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

    public class CandlesHistoryRequest
    {
        [Required]
        public Period? Period { get; set; }

        [Required]
        public PriceType? Type { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime? DateFrom { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [DateGreaterThan("DateFrom")]
        public DateTime? DateTo { get; set; }
    }

    public static class RequestsConvertorsExt
    {
        public static TimeInterval ToDomainModel(this Period candleType)
        {
            switch (candleType)
            {
                case Period.Sec:
                    return TimeInterval.Sec;
                case Period.Minute:
                    return TimeInterval.Minute;
                case Period.Hour:
                    return TimeInterval.Hour;
                case Period.Day:
                    return TimeInterval.Day;
                case Period.Month:
                    return TimeInterval.Month;
                default:
                    throw new ArgumentOutOfRangeException(nameof(candleType), candleType, null);
            }
        }

        public static Lykke.Domain.Prices.PriceType ToDomainModel(this PriceType type)
        {
            return (Lykke.Domain.Prices.PriceType)type;
        }
    }
}
