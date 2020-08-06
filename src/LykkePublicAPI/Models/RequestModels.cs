using System;
using System.ComponentModel.DataAnnotations;
using Lykke.Service.CandlesHistory.Client.Models;

namespace LykkePublicAPI.Models
{
    public enum MarketType
    {
        Spot,
        Mt
    }

    public enum Period
    {
        Min5,
        Min15,
        Min30,
        Hour,
        Hour4,
        Hour6,
        Hour12,
        Day,
        Week,
        Month
    }

    public enum PriceType
    {
        Bid = 1,
        Ask = 2,
        Mid = 3,
        Trades = 4
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

        public static CandleTimeInterval ToCandlesHistoryServiceApiModel(this Period candleType)
        {
            switch (candleType)
            {
                case Period.Min5:
                    return CandleTimeInterval.Min5;
                case Period.Min15:
                    return CandleTimeInterval.Min15;
                case Period.Min30:
                    return CandleTimeInterval.Min30;
                case Period.Hour:
                    return CandleTimeInterval.Hour;
                case Period.Hour4:
                    return CandleTimeInterval.Hour4;
                case Period.Hour6:
                    return CandleTimeInterval.Hour6;
                case Period.Hour12:
                    return CandleTimeInterval.Hour12;
                case Period.Day:
                    return CandleTimeInterval.Day;
                case Period.Week:
                    return CandleTimeInterval.Week;
                case Period.Month:
                    return CandleTimeInterval.Month;
                default:
                    throw new ArgumentOutOfRangeException(nameof(candleType), candleType, null);
            }
        }

        public static Lykke.Domain.Prices.TimeInterval ToDomainModel(this Period candleType)
        {
            switch (candleType)
            {
                case Period.Min5:
                    return Lykke.Domain.Prices.TimeInterval.Min5;
                case Period.Min15:
                    return Lykke.Domain.Prices.TimeInterval.Min15;
                case Period.Min30:
                    return Lykke.Domain.Prices.TimeInterval.Min30;
                case Period.Hour:
                    return Lykke.Domain.Prices.TimeInterval.Hour;
                case Period.Hour4:
                    return Lykke.Domain.Prices.TimeInterval.Hour4;
                case Period.Hour6:
                    return Lykke.Domain.Prices.TimeInterval.Hour6;
                case Period.Hour12:
                    return Lykke.Domain.Prices.TimeInterval.Hour12;
                case Period.Day:
                    return Lykke.Domain.Prices.TimeInterval.Day;
                case Period.Week:
                    return Lykke.Domain.Prices.TimeInterval.Week;
                case Period.Month:
                    return Lykke.Domain.Prices.TimeInterval.Month;
                default:
                    throw new ArgumentOutOfRangeException(nameof(candleType), candleType, null);
            }
        }

        public static CandlePriceType ToCandlesHistoryServiceModel(this PriceType type)
        {
            switch (type)
            {
                case PriceType.Bid:
                    return CandlePriceType.Bid;
                case PriceType.Ask:
                    return CandlePriceType.Ask;
                case PriceType.Mid:
                    return CandlePriceType.Mid;
                case PriceType.Trades:
                    return CandlePriceType.Trades;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static Core.Domain.Market.MarketType ToDomain(this MarketType marketType)
        {
            switch (marketType)
            {
                case MarketType.Spot:
                    return Core.Domain.Market.MarketType.Spot;
                case MarketType.Mt:
                    return Core.Domain.Market.MarketType.Mt;
                default:
                    throw new ArgumentOutOfRangeException(nameof(marketType), marketType, null);
            }
        }
    }
}
