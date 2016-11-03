using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Reports;
using Core.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Reports
{
    public class TradeVolumesRecord : TableEntity, ITradeVolumesRecord
    {
        public static string GeneratePartition(string assetId)
        {
            return assetId;
        }

        public string Asset { get; set; }
        public double TotalVolume { get; set; }
        public int TradesCount { get; set; }
    }

    public class TradeVolumesRepository : ITradeVolumesRepository
    {
        private readonly INoSQLTableStorage<TradeVolumesRecord> _tableStorage;

        public TradeVolumesRepository(INoSQLTableStorage<TradeVolumesRecord> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<ITradeVolumesRecord> GetLastRecord(string assetId)
        {
            return await _tableStorage.GetTopRecordAsync(TradeVolumesRecord.GeneratePartition(assetId));
        }

        public async Task<IEnumerable<ITradeVolumesRecord>> GetLastRecords(string[] assetIds)
        {
            var tasks = assetIds.Select(GetLastRecord);
            return (await Task.WhenAll(tasks)).Where(x => x != null);
        }
    }
}
