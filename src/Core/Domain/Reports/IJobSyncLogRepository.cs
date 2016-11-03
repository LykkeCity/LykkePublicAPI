using System.Threading.Tasks;

namespace Core.Domain.Reports
{
    public interface IJobSyncLogRepository
    {
        Task<int> GetTradeReportsTableVersion();
    }
}
