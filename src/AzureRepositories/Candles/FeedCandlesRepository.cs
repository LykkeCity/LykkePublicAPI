using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AzureStorage;
using Core.Domain.Candles;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

        public static FeedCandleEntity Create(IFeedCandle src, FeedCandleType feedCandleType, string assetPairId)
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
                Time = src.Time,
            };
        }

        public void ParseCandles(FeedCandleType type)
        {
            Candles = new List<FeedCandle>();
            List<FeedCandleEntity> candles = JsonConvert.DeserializeObject<List<FeedCandleEntity>>(Data);

            foreach (var candle in candles)
            {
                candle.IsBuy = IsBuy;
                candle.DateTime = candle.GetCandleDateTime(DateTime, type);
                Candles.Add(FeedCandle.Create(candle));
            }
        }

        public override string ToString()
        {
            return $"O: {Open}, C: {Close}, H: {High}, L: {Low}, IsBuy: {IsBuy}, T: {DateTime:u}{Environment.NewLine}Data: {Data}";
        }
    }

    public class FeedCandlesRepository : IFeedCandlesRepository
    {
        private readonly INoSQLTableStorage<FeedCandleEntity> _tableStorage;

        public FeedCandlesRepository(INoSQLTableStorage<FeedCandleEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IFeedCandle> ReadCandleAsync(string assertPairId, FeedCandleType feedCandleType, bool isBuy, DateTime date)
        {
            var feedCandleEntity = await _tableStorage.GetDataAsync(
                        FeedCandleEntity.GeneratePartitionKey(assertPairId, isBuy, feedCandleType),
                        FeedCandleEntity.GenerateRowKey(date, feedCandleType));

            feedCandleEntity?.ParseCandles(feedCandleType);

            return feedCandleEntity;
        }

        public async Task<IEnumerable<IFeedCandle>> ReadCandlesAsync(string assertPairId, FeedCandleType feedCandleType, DateTime from, DateTime to, bool isBuy)
        {
            var candleEntities = await _tableStorage.WhereAsync(FeedCandleEntity.GeneratePartitionKey(assertPairId, isBuy, feedCandleType), from, to, ToIntervalOption.IncludeTo);
            var candles = candleEntities.OrderByDescending(item => item.DateTime).ToList();

            var result = new List<IFeedCandle>();

            foreach (var candle in candles)
            {
                candle.ParseCandles(feedCandleType);
                result.Add(FeedCandle.Create(candle));
            }

            return result;
        }

        public async Task<IEnumerable<IFeedCandle>> ReadCandlesAsync(string assetPairId, FeedCandleType feedCandleType, DateTime candleDate, bool isBuy)
        {
            FeedCandleEntity candle = await _tableStorage.GetDataAsync(FeedCandleEntity.GeneratePartitionKey(assetPairId, isBuy, feedCandleType), FeedCandleEntity.GenerateRowKey(candleDate, feedCandleType));

            if (candle != null)
            {
                candle.ParseCandles(feedCandleType);
                return candle.Candles;
            }

            return new List<IFeedCandle>();
        }
    }

    public class CandleContractResolver : DefaultContractResolver
    {
        private readonly List<string> _names = new List<string> { "O", "C", "H", "L", "T" };

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            property.ShouldSerialize = o => _names.Contains(property.PropertyName);

            return property;
        }
    }
}
