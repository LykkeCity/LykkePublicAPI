using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain.Market;

namespace Core.Services
{
    public interface IMarketTradingDataService
    {
        Task<AssetPairTradingDataItem<double>> TryGetPairVolumeAsync(string assetPair);
        Task<AssetPairTradingDataItem<double>> TryGetPairLastTradePriceAsync(string assetPair);
        Task<IEnumerable<AssetPairTradingDataItem<double>>> TryGetAllPairsVolumeAsync();
        Task<IEnumerable<AssetPairTradingDataItem<double>>> TryGetAllPairsLastTradePriceAsync();
    }
}
