using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Domain.OrderBook
{
    public interface IOrderBook
    {
        string AssetPair { get; }
        bool IsBuy { get; }
        DateTime Timestamp { get; }
        List<VolumePrice> Prices { get; }
    }

    public class OrderBook : IOrderBook
    {
        public string AssetPair { get; set; }
        public bool IsBuy { get; set; }
        public DateTime Timestamp { get; set; }
        public List<VolumePrice> Prices { get; set; } = new List<VolumePrice>();
    }

    public class VolumePrice
    {
        public double Volume { get; set; }
        public double Price { get; set; }
    }

    public static class OrderBookExt
    {
        public static double GetPrice(this IOrderBook src)
        {
            return src.IsBuy
                ? src.Prices.Max(item => item.Price)
                : src.Prices.Min(item => item.Price);
        }
    }
}
