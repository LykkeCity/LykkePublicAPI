using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Domain.Reports
{
    public interface ITradeVolumesRecord
    {
        string Asset { get; set; }
        double TotalVolume { get; set; }
        int TradesCount { get; set; }
    }

    public interface ITradeVolumesRepository
    {
        Task<ITradeVolumesRecord> GetLastRecord(string assetId);
        Task<IEnumerable<ITradeVolumesRecord>> GetLastRecords(string[] assetIds);
    }
}
