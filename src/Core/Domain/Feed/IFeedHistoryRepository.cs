using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Feed
{
    public class TradeCandle
    {
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public int Seconds { get; set; }
    }

    public interface IFeedHistory
    {
        string AssetPair { get; }
        string PriceType { get; }
        DateTime FeedTime { get; }
        TradeCandle[] TradeCandles { get; }
    }

    public static class TradePriceType
    {
        public const string Bid = "Bid";
        public const string Ask = "Ask";
        public const string Both = "Both";
    }

    public interface IFeedHistoryRepository
    {
        Task<IFeedHistory> GetAsync(string assetPairId, string priceType, DateTime feedTime);
        Task<IEnumerable<IFeedHistory>> GetAsync(string assetPairId, string priceType, DateTime from, DateTime to);
        Task<IEnumerable<IFeedHistory>> GetLastTenMinutesAskAsync(string assetPairId);

        // temporary due to missed data in FeedHistory
        Task<IFeedHistory> GetСlosestAvailableAsync(string assetPairId, string priceType, DateTime feedTime);
    }
}
