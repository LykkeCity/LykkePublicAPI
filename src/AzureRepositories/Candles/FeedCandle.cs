using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Domain.Prices.Contracts;

namespace AzureRepositories.Candles
{
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

        public bool Equals(IFeedCandle other)
        {
            if (other != null)
            {
                return this.DateTime == other.DateTime
                    && this.Open == other.Open
                    && this.Close == other.Close
                    && this.High == other.High
                    && this.Low == other.Low
                    && this.IsBuy == other.IsBuy;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.DateTime.GetHashCode()
                ^ this.IsBuy.GetHashCode()
                ^ this.Open.GetHashCode()
                ^ this.Close.GetHashCode()
                ^ this.High.GetHashCode()
                ^ this.Low.GetHashCode();
        }

        public override string ToString()
        {
            return $"O: {Open}, C: {Close}, H: {High}, L: {Low}, IsBuy: {IsBuy}, T: {DateTime:u}";
        }
    }
}
