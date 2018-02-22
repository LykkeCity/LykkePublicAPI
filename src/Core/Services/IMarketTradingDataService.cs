using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain.Market;

namespace Core.Services
{
    public interface IMarketTradingDataService
    {
        Task<AssetPairTradingData> TryGetPairAsync(string assetPair);
        Task<IEnumerable<AssetPairTradingData>> GetAllPairsAsync();
    }
}
