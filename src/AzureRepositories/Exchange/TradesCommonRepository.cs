using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Domain.Exchange;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Exchange
{
    public class TradeCommonEntity : TableEntity, ITradeCommon
    {
        public static string GenerateParitionKey(DateTime dt)
        {
            return $"{dt.Year}-{dt.Month}-{dt.Day}";
        }

        public static string GenerateRowKey(DateTime dt, string id)
        {
            return $"{DateTime.MaxValue.Ticks - dt.Ticks}_{id}";
        }

        public string Id { get; set; }
        public DateTime Dt { get; set; }
        public string AssetPair { get; set; }
        public string BaseAsset { get; set; }
        public string QuotAsset { get; set; }
        public double Price { get; set; }
        public double Amount { get; set; }
        public string LimitOrderId { get; set; }
        public string MarketOrderId { get; set; }
    }

    public class TradesCommonRepository : ITradesCommonRepository
    {
        private readonly INoSQLTableStorage<TradeCommonEntity> _tableStorage;
        private const int DaysLimit = 7;

        public TradesCommonRepository(INoSQLTableStorage<TradeCommonEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<ITradeCommon>> GetLastTrades(int n)
        {
            List<ITradeCommon> trades = new List<ITradeCommon>();
            var currentDt = DateTime.UtcNow;
            for (int i = 0; i < DaysLimit; ++i)
            {
                trades.AddRange(await _tableStorage.GetTopRecordsAsync(TradeCommonEntity.GenerateParitionKey(currentDt), n));

                if (trades.Count == n)
                    return trades;

                currentDt = currentDt.Subtract(TimeSpan.FromDays(1));
            }

            return trades;
        }
    }
}
