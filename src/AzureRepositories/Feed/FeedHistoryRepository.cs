using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Core.Feed;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Feed
{
    public static class PriceType
    {
        public const string Bid = "Bid";
        public const string Ask = "Ask";
    }

    public class FeedHistoryEntity : TableEntity
    {
        public string Data { get; set; }

        public static string GeneratePartition(string assetId, string priceType)
        {
            return $"{assetId}_{priceType}";
        }

        public static string GenerateRowKey(DateTime feedTime)
        {
            return $"{feedTime.Year}{feedTime.Month.ToString("00")}{feedTime.Day.ToString("00")}{feedTime.Hour.ToString("00")}{feedTime.Minute.ToString("00")}";
        }
    }

    public class FeedHistoryDto : IFeedHistory
    {
        public string AssetPair { get; set; }
        public string PriceType { get; set; }
        public DateTime FeedTime { get; set; }
        public TradeCandle[] TradeCandles { get; set; }
    }

    public static class FeedHistoryExt
    {
        public static FeedHistoryDto ToDto(this FeedHistoryEntity entity)
        {
            var dto = new FeedHistoryDto();

            //example: "BTCCHF_Bid"
            var assetPriceTypeVals = entity.PartitionKey.Split('_');
            dto.AssetPair = assetPriceTypeVals[0];
            dto.PriceType = assetPriceTypeVals[1];

            dto.TradeCandles = ParseCandles(entity.Data);
            dto.FeedTime = ParseFeedTime(entity.RowKey);

            return dto;
        }

        public static DateTime ParseFeedTime(string rowKey)
        {
            //example: 201604290745
            int year = int.Parse(rowKey.Substring(0, 4));
            int month = int.Parse(rowKey.Substring(4, 2));
            int day = int.Parse(rowKey.Substring(6, 2));
            int hour = int.Parse(rowKey.Substring(8, 2));
            int min = int.Parse(rowKey.Substring(10, 2));
            return new DateTime(year, month, day, hour, min, 0);
        }

        private static TradeCandle[] ParseCandles(string data)
        {
            var candlesList = new List<TradeCandle>();
            if (!string.IsNullOrEmpty(data))
            {
                var candles = data.Split('|');
                foreach (var candle in candles)
                {
                    if (!string.IsNullOrEmpty(candle))
                    {
                        //parameters example: O=446.322;C=446.322;H=446.322;L=446.322;T=30
                        var parameters = candle.Split(';');

                        var tradeCandle = new TradeCandle();
                        foreach (var nameValuePair in parameters.Select(parameter => parameter.Split('=')))
                        {
                            switch (nameValuePair[0])
                            {
                                case "O":
                                    tradeCandle.Open = nameValuePair[1].ParseAnyDouble();
                                    break;
                                case "C":
                                    tradeCandle.Close = nameValuePair[1].ParseAnyDouble();
                                    break;
                                case "H":
                                    tradeCandle.High = nameValuePair[1].ParseAnyDouble();
                                    break;
                                case "L":
                                    tradeCandle.Low = nameValuePair[1].ParseAnyDouble();
                                    break;
                                case "T":
                                    tradeCandle.Seconds = int.Parse(nameValuePair[1]);
                                    break;
                                default:
                                    throw new ArgumentException("unexpected value");
                            }
                        }
                        candlesList.Add(tradeCandle);
                    }
                }
            }

            return candlesList.ToArray();
        }
    }

    public class FeedHistoryRepository : IFeedHistoryRepository
    {
        private readonly INoSQLTableStorage<FeedHistoryEntity> _tableStorage;

        public FeedHistoryRepository(INoSQLTableStorage<FeedHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IFeedHistory> GetAsync(string assetPairId, string priceType, DateTime feedTime)
        {
            var entity = await _tableStorage.GetDataAsync(FeedHistoryEntity.GeneratePartition(assetPairId, priceType),
                FeedHistoryEntity.GenerateRowKey(feedTime));
            return entity?.ToDto();
        }

        public async Task<IEnumerable<IFeedHistory>> GetAsync(string assetPairId, string priceType,
            DateTime @from, DateTime to)
        {
            var entities = await _tableStorage.GetDataAsync(FeedHistoryEntity.GeneratePartition(assetPairId, priceType),
                entity =>
                {
                    var dt = FeedHistoryExt.ParseFeedTime(entity.RowKey);
                    return dt > @from && dt < to;
                });
            return entities.Select(x => x.ToDto());
        }

        public async Task<IEnumerable<IFeedHistory>> GetLastTenMinutesAskAsync(string assetPairId)
        {
            var rangeQuery = AzureStorageUtils.QueryGenerator<FeedHistoryEntity>
                .GreaterThanQuery(FeedHistoryEntity.GeneratePartition(assetPairId, PriceType.Ask),
                FeedHistoryEntity.GenerateRowKey(DateTime.UtcNow.AddMinutes(-10)));

            return (await _tableStorage.WhereAsync(rangeQuery)).Select(x => x.ToDto());
        }

        public async Task<IFeedHistory> GetСlosestAvailableAsync(string assetPairId, string priceType, DateTime feedTime)
        {
            var rangeQuery = AzureStorageUtils.QueryGenerator<FeedHistoryEntity>
                            .GreaterThanQuery(FeedHistoryEntity.GeneratePartition(assetPairId, priceType),
                            FeedHistoryEntity.GenerateRowKey(feedTime)).Take(1);

            var resList = new List<FeedHistoryEntity>();
            await _tableStorage.ExecuteAsync(rangeQuery, entities =>
            {
                resList.AddRange(entities);
            }, () => false);

            return resList.First().ToDto();
        }
    }
}
