using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Domain.Prices.Contracts;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Lykke.Domain.Prices;

namespace AzureRepositories.Candles
{
    public class FeedCandleEntity : TableEntity, IFeedCandle
    {
        public string Data { get; set; }
        [JsonIgnore]
        public DateTime DateTime { get; set; }
        [JsonProperty("O")]
        public double Open { get; set; }
        [JsonProperty("C")]
        public double Close { get; set; }
        [JsonProperty("H")]
        public double High { get; set; }
        [JsonProperty("L")]
        public double Low { get; set; }
        [JsonIgnore]
        public bool IsBuy { get; set; }
        [JsonIgnore]
        public List<FeedCandle> Candles { get; set; }
        [JsonProperty("T")]
        public int Time { get; set; }

        public static string GeneratePartitionKey(string assetPairId, bool isBuy, FeedCandleType feedCandleType)
        {
            return $"{assetPairId}_{(isBuy ? "BUY" : "SELL")}_{feedCandleType}";
        }

        public static string GenerateRowKey(DateTime date, FeedCandleType feedCandleType)
        {
            string rowKey;

            switch (feedCandleType)
            {
                case FeedCandleType.Month:
                    rowKey = $"{date.Year}";
                    break;
                case FeedCandleType.Day:
                    rowKey = $"{date.Year}-{date.Month:00}";
                    break;
                case FeedCandleType.Hour:
                    rowKey = $"{date.Year}-{date.Month:00}-{date.Day:00}";
                    break;
                case FeedCandleType.Minute:
                    rowKey = $"{date.Year}-{date.Month:00}-{date.Day:00}T{date.Hour:00}";
                    break;
                case FeedCandleType.Sec:
                    rowKey = $"{date.Year}-{date.Month:00}-{date.Day:00}T{date.Hour:00}:{date.Minute:00}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(feedCandleType), feedCandleType, null);
            }

            return rowKey;
        }

        public static FeedCandleEntity Create(IFeedCandle src, FeedCandleType feedCandleType, string assetPairId, int time)
        {
            return new FeedCandleEntity
            {
                PartitionKey = GeneratePartitionKey(assetPairId, src.IsBuy, feedCandleType),
                RowKey = GenerateRowKey(src.DateTime, feedCandleType),
                DateTime = src.DateTime,
                Open = src.Open,
                Close = src.Close,
                High = src.High,
                Low = src.Low,
                IsBuy = src.IsBuy,
                Time = time,
            };
        }

        public void ParseCandles(FeedCandleType type)
        {
            Candles = new List<FeedCandle>();
            List<FeedCandleEntity> candles = JsonConvert.DeserializeObject<List<FeedCandleEntity>>(Data);

            foreach (FeedCandleEntity candle in candles)
            {
                candle.IsBuy = IsBuy;
                candle.DateTime = candle.GetCandleDateTime(DateTime, type, candle.Time);
                Candles.Add(FeedCandle.Create(candle));
            }
        }

        public override string ToString()
        {
            return $"O: {Open}, C: {Close}, H: {High}, L: {Low}, IsBuy: {IsBuy}, T: {DateTime:u}{Environment.NewLine}Data: {Data}";
        }
    }
}
