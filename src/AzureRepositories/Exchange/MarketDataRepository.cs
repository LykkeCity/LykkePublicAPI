using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Domain.Exchange;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Exchange
{
    public class MarketDataEntity : TableEntity, IMarketData
    {
        public string AssetPairId { get; set; }
        public double Volume { get; set; }
        public double LastPrice { get; set; }
        public DateTime Dt { get; set; }

        public static string GeneratePartition()
        {
            return "md";
        }

        public static string GenerateRowKey(string assetPairId)
        {
            return assetPairId;
        }

        public static MarketDataEntity Create(IMarketData md)
        {
            return new MarketDataEntity
            {
                AssetPairId = md.AssetPairId,
                Dt = md.Dt,
                LastPrice = md.LastPrice,
                Volume = md.Volume,
                PartitionKey = GeneratePartition(),
                RowKey = GenerateRowKey(md.AssetPairId)
            };
        }
    }

    public class MarketDataRepository : IMarketDataRepository
    {
        private readonly INoSQLTableStorage<MarketDataEntity> _tableStorage;

        public MarketDataRepository(INoSQLTableStorage<MarketDataEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<IMarketData>> Get24HMarketsAsync()
        {
            var records = (await _tableStorage.GetDataAsync(MarketDataEntity.GeneratePartition())).ToArray();

            foreach (var record in records)
            {
                if (record.Dt < DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)))
                {
                    record.Volume = 0;
                }
            }

            return records;
        }
    }
}
