using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Exchange;
using Core.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Exchange
{
    public class MarketDataEntity : TableEntity, IMarketData
    {
        public string AssetPairId { get; set; }
        public double LkkVolume { get; set; }
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
                LkkVolume = md.LkkVolume,
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

        public Task AddOrReplaceMarketData(IEnumerable<IMarketData> data)
        {
            var entities = data.Select(MarketDataEntity.Create);
            return _tableStorage.InsertAsync(entities);
        }

        public async Task<IEnumerable<IMarketData>> Get24HMarketsAsync()
        {
            var records = await _tableStorage.GetDataAsync(MarketDataEntity.GeneratePartition());
            return records.Where(x => x.Dt > DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)));
        }
    }
}
