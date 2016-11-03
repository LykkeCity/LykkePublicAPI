using System.Threading.Tasks;
using Core.Domain.Reports;
using Core.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Reports
{
    public enum OperationType
    {
        Begin = 0,
        End = 3,
        Deleted = 4
    }

    public class JobSyncLogRecord : TableEntity
    {
        public static string GeneratePartition()
        {
            return "AssetsSummariesPerDay";
        }

        public int Version { get; set; }
        public int Operation { get; set; }

        public OperationType OperationType => (OperationType) Operation;
    }

    public class JobSyncLogRepository : IJobSyncLogRepository
    {
        private readonly INoSQLTableStorage<JobSyncLogRecord> _tableStorage;

        public JobSyncLogRepository(INoSQLTableStorage<JobSyncLogRecord> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<int> GetTradeReportsTableVersion()
        {
            var entity = await _tableStorage.GetTopRecordAsync(JobSyncLogRecord.GeneratePartition());
            return entity.OperationType == OperationType.End ? entity.Version : entity.Version - 1;
        }
    }
}
