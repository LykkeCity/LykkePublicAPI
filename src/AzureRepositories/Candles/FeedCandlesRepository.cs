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
using Lykke.Domain.Prices.Contracts;
using Lykke.Domain.Prices;

namespace AzureRepositories.Candles
{
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

    internal class CandleContractResolver : DefaultContractResolver
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
