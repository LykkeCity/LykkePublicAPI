using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Domain.Candles
{
    public enum FeedCandleType
    {
        Sec,
        Minute,
        Hour,
        Day,
        Month
    }

    public interface IFeedCandle
    {
        DateTime DateTime { get; }
        double Open { get; }
        double Close { get; }
        double High { get; }
        double Low { get; }
        bool IsBuy { get; }
        int Time { get; }
    }

    public class FeedCandle : IFeedCandle
    {
        public DateTime DateTime { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public bool IsBuy { get; set; }
        public int Time { get; set; }

        public static FeedCandle Create(IFeedCandle src)
        {
            return Create(src.DateTime, src.Open, src.Close, src.Low, src.High, src.IsBuy);
        }

        public static FeedCandle Create(DateTime dateTime, double open, double close, double low, double high, bool isBuy)
        {
            return new FeedCandle
            {
                DateTime = dateTime,
                Open = open,
                Close = close,
                High = high,
                Low = low,
                IsBuy = isBuy
            };
        }

        public override string ToString()
        {
            return $"O: {Open}, C: {Close}, H: {High}, L: {Low}, IsBuy: {IsBuy}, T: {DateTime:u}";
        }
    }

    public interface IFeedCandlesRepository
    {
        Task<IFeedCandle> ReadCandleAsync(string assertPairId, FeedCandleType feedCandleType, bool isBuy, DateTime date);

        Task<IEnumerable<IFeedCandle>> ReadCandlesAsync(string assertPairId, FeedCandleType feedCandleType,
            DateTime from, DateTime to, bool isBuy);

        Task<IEnumerable<IFeedCandle>> ReadCandlesAsync(string assetPairId, FeedCandleType feedCandleType,
            DateTime candleDate, bool isBuy);
    }

    public static class FeedCandleExt
    {
        public static int GetCandleTime(this IFeedCandle src, FeedCandleType feedCeCandleType)
        {
            switch (feedCeCandleType)
            {
                case FeedCandleType.Month:
                    return src.DateTime.Month;

                case FeedCandleType.Day:
                    return src.DateTime.Day;

                case FeedCandleType.Hour:
                    return src.DateTime.Hour;

                case FeedCandleType.Minute:
                    return src.DateTime.Minute;

                case FeedCandleType.Sec:
                    return src.DateTime.Second;

                default:
                    throw new ArgumentOutOfRangeException(nameof(feedCeCandleType), feedCeCandleType, null);
            }
        }

        public static DateTime GetCandleDateTime(this IFeedCandle src, DateTime date, FeedCandleType feedCeCandleType)
        {
            switch (feedCeCandleType)
            {
                case FeedCandleType.Month:
                    return new DateTime(date.Year, src.Time, date.Day, date.Hour, date.Minute, date.Second);

                case FeedCandleType.Day:
                    return new DateTime(date.Year, date.Month, src.Time, date.Hour, date.Minute, date.Second);

                case FeedCandleType.Hour:
                    return new DateTime(date.Year, date.Month, date.Day, src.Time, date.Minute, date.Second);

                case FeedCandleType.Minute:
                    return new DateTime(date.Year, date.Month, date.Day, date.Hour, src.Time, date.Second);

                case FeedCandleType.Sec:
                    return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, src.Time);

                default:
                    throw new ArgumentOutOfRangeException(nameof(feedCeCandleType), feedCeCandleType, null);
            }
        }
    }
}
