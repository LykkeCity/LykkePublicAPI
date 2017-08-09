using System;
using System.ComponentModel.DataAnnotations;
using Lykke.Service.CandlesHistory.Client.Models;

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

        public static TimeInterval ToCandlesHistoryServiceApiModel(this Period candleType)
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

        public static Lykke.Domain.Prices.TimeInterval ToDomainModel(this Period candleType)
        {
            switch (candleType)
            {
                case Period.Sec:
                    return Lykke.Domain.Prices.TimeInterval.Sec;
                case Period.Minute:
                    return Lykke.Domain.Prices.TimeInterval.Minute;
                case Period.Hour:
                    return Lykke.Domain.Prices.TimeInterval.Hour;
                case Period.Day:
                    return Lykke.Domain.Prices.TimeInterval.Day;
                case Period.Month:
                    return Lykke.Domain.Prices.TimeInterval.Month;
                default:
                    throw new ArgumentOutOfRangeException(nameof(candleType), candleType, null);
            }
        }

        public static Lykke.Service.CandlesHistory.Client.Models.PriceType ToCandlesHistoryServiceModel(this PriceType type)
        {
            switch (type)
            {
                case PriceType.Bid:
                    return Lykke.Service.CandlesHistory.Client.Models.PriceType.Bid;
                case PriceType.Ask:
                    return Lykke.Service.CandlesHistory.Client.Models.PriceType.Ask;
                case PriceType.Mid:
                    return Lykke.Service.CandlesHistory.Client.Models.PriceType.Mid;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
