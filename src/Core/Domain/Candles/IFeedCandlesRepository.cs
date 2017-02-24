using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices;
using Lykke.Domain.Prices.Contracts;

namespace Core.Domain.Candles
{
    public interface IFeedCandlesRepository
    {
        Task<IFeedCandle> ReadCandleAsync(string assertPairId, FeedCandleType feedCandleType, bool isBuy, DateTime date);

        Task<IEnumerable<IFeedCandle>> ReadCandlesAsync(string assertPairId, FeedCandleType feedCandleType,
            DateTime from, DateTime to, bool isBuy);

        Task<IEnumerable<IFeedCandle>> ReadCandlesAsync(string assetPairId, FeedCandleType feedCandleType,
            DateTime candleDate, bool isBuy);
    }
}
