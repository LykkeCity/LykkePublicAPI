using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace AzureRepositories.Candles
{
    public static class FeedCandleExt
    {
        public static int GetCandleTime(this IFeedCandle src, FeedCandleType feedCandleType)
        {
            switch (feedCandleType)
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
                    throw new ArgumentOutOfRangeException(nameof(feedCandleType), feedCandleType, null);
            }
        }

        public static DateTime GetCandleDateTime(this IFeedCandle src, DateTime date, FeedCandleType feedCandleType, int time)
        {
            switch (feedCandleType)
            {
                case FeedCandleType.Month:
                    return new DateTime(date.Year, time, date.Day, date.Hour, date.Minute, date.Second);

                case FeedCandleType.Day:
                    return new DateTime(date.Year, date.Month, time, date.Hour, date.Minute, date.Second);

                case FeedCandleType.Hour:
                    return new DateTime(date.Year, date.Month, date.Day, time, date.Minute, date.Second);

                case FeedCandleType.Minute:
                    return new DateTime(date.Year, date.Month, date.Day, date.Hour, time, date.Second);

                case FeedCandleType.Sec:
                    return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, time);

                default:
                    throw new ArgumentOutOfRangeException(nameof(feedCandleType), feedCandleType, null);
            }
        }
    }
}
